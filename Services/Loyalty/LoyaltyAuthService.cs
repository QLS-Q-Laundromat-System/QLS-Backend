using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QLS.Backend.Data;
using QLS.Backend.DTOs.Loyalty.Auth;
using QLS.Backend.Exceptions;
using QLS.Backend.Interfaces.Loyalty;
using QLS.Backend.Models;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Services.Loyalty
{
    public class LoyaltyAuthService : ILoyaltyAuthService
    {
        private const int MaximumOtpAttempts = 5;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILoyaltyOtpDeliveryService _otpDeliveryService;

        public LoyaltyAuthService(
            AppDbContext context,
            IConfiguration configuration,
            ILoyaltyOtpDeliveryService otpDeliveryService)
        {
            _context = context;
            _configuration = configuration;
            _otpDeliveryService = otpDeliveryService;
        }

        public async Task<LoyaltyOtpRequestResponseDto> RequestOtpAsync(LoyaltyOtpRequestDto request)
        {
            await EnsureBrandExistsAsync(request.BrandId);

            var identifier = NormalizeIdentifier(request.Identifier);
            var purpose = NormalizePurpose(request.Purpose);
            var customer = await FindCustomerAsync(request.BrandId, identifier);

            if (purpose == "Register" && customer != null)
            {
                throw new ApiException("Email hoặc số điện thoại đã được đăng ký.", 409);
            }

            var now = DateTime.UtcNow;
            var cooldownSeconds = GetPositiveConfiguration("LoyaltyAuth:OtpCooldownSeconds", 60);
            var latestChallenge = await _context.LoyaltyOtpChallenges
                .Where(c =>
                    c.BrandId == request.BrandId &&
                    c.Identifier == identifier.Value &&
                    c.Purpose == purpose)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestChallenge != null && latestChallenge.CreatedAt.AddSeconds(cooldownSeconds) > now)
            {
                throw new ApiException("Vui lòng chờ trước khi yêu cầu mã OTP mới.", 429);
            }

            var otpCode = RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");
            var expiresAt = now.AddMinutes(GetPositiveConfiguration("LoyaltyAuth:OtpTtlMinutes", 5));
            var challenge = new LoyaltyOtpChallenge
            {
                BrandId = request.BrandId,
                Identifier = identifier.Value,
                Channel = identifier.Channel,
                Purpose = purpose,
                CodeHash = BCrypt.Net.BCrypt.HashPassword(otpCode),
                ExpiresAt = expiresAt,
                CreatedAt = now
            };

            if (purpose == "Login" && customer == null)
            {
                return new LoyaltyOtpRequestResponseDto
                {
                    Channel = identifier.Channel,
                    ExpiresAt = expiresAt
                };
            }

            _context.LoyaltyOtpChallenges.Add(challenge);
            await _context.SaveChangesAsync();
            await _otpDeliveryService.SendAsync(identifier.Channel, identifier.Value, otpCode);

            return new LoyaltyOtpRequestResponseDto
            {
                Channel = identifier.Channel,
                ExpiresAt = expiresAt,
                DevelopmentOtpCode = _configuration.GetValue<bool>("LoyaltyAuth:ExposeOtpCodeInResponse")
                    ? otpCode
                    : null
            };
        }

        public async Task<LoyaltyAuthResponseDto> RegisterAsync(LoyaltyRegisterRequestDto request)
        {
            await EnsureBrandExistsAsync(request.BrandId);

            var identifier = NormalizeIdentifier(request.Identifier);
            if (await FindCustomerAsync(request.BrandId, identifier) != null)
            {
                throw new ApiException("Email hoặc số điện thoại đã được đăng ký.", 409);
            }

            await ConsumeOtpAsync(request.BrandId, identifier.Value, "Register", request.OtpCode);

            var now = DateTime.UtcNow;
            var customer = new LoyaltyCustomer
            {
                BrandId = request.BrandId,
                Email = identifier.Channel == "Email" ? identifier.Value : null,
                PhoneNumber = identifier.Channel == "Phone" ? identifier.Value : null,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsEmailVerified = identifier.Channel == "Email",
                IsPhoneNumberVerified = identifier.Channel == "Phone",
                FullName = request.FullName.Trim(),
                CustomerType = CustomerType.Member,
                StudentVerificationStatus = StudentVerificationStatus.None,
                TotalPoints = 0,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.LoyaltyCustomers.Add(customer);
            await _context.SaveChangesAsync();
            return CreateAuthResponse(customer);
        }

        public async Task<LoyaltyAuthResponseDto> LoginWithPasswordAsync(LoyaltyPasswordLoginRequestDto request)
        {
            var identifier = NormalizeIdentifier(request.Identifier);
            var customer = await FindCustomerAsync(request.BrandId, identifier);

            if (customer == null ||
                !IsIdentifierVerified(customer, identifier.Channel) ||
                !BCrypt.Net.BCrypt.Verify(request.Password, customer.PasswordHash))
            {
                throw new ApiException("Thông tin đăng nhập không chính xác.", 401);
            }

            return CreateAuthResponse(customer);
        }

        public async Task<LoyaltyAuthResponseDto> LoginWithOtpAsync(LoyaltyOtpLoginRequestDto request)
        {
            var identifier = NormalizeIdentifier(request.Identifier);
            var customer = await FindCustomerAsync(request.BrandId, identifier);

            if (customer == null || !IsIdentifierVerified(customer, identifier.Channel))
            {
                throw new ApiException("Thông tin đăng nhập không chính xác.", 401);
            }

            await ConsumeOtpAsync(request.BrandId, identifier.Value, "Login", request.OtpCode);
            return CreateAuthResponse(customer);
        }

        private async Task ConsumeOtpAsync(Guid brandId, string identifier, string purpose, string otpCode)
        {
            var challenge = await _context.LoyaltyOtpChallenges
                .Where(c =>
                    c.BrandId == brandId &&
                    c.Identifier == identifier &&
                    c.Purpose == purpose &&
                    c.ConsumedAt == null)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            if (challenge == null || challenge.ExpiresAt <= DateTime.UtcNow)
            {
                throw new ApiException("Mã OTP không tồn tại hoặc đã hết hạn.", 400);
            }

            if (challenge.FailedAttempts >= MaximumOtpAttempts)
            {
                throw new ApiException("Mã OTP đã bị khóa do nhập sai quá nhiều lần.", 429);
            }

            if (!BCrypt.Net.BCrypt.Verify(otpCode, challenge.CodeHash))
            {
                challenge.FailedAttempts++;
                await _context.SaveChangesAsync();
                throw new ApiException("Mã OTP không chính xác.", 400);
            }

            challenge.ConsumedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        private async Task EnsureBrandExistsAsync(Guid brandId)
        {
            if (!await _context.Brands.AnyAsync(b => b.Id == brandId && b.IsActive))
            {
                throw new ApiException("Brand không tồn tại hoặc đã bị khóa.", 404);
            }
        }

        private Task<LoyaltyCustomer?> FindCustomerAsync(Guid brandId, NormalizedIdentifier identifier)
        {
            return identifier.Channel == "Email"
                ? _context.LoyaltyCustomers.FirstOrDefaultAsync(c => c.BrandId == brandId && c.Email == identifier.Value)
                : _context.LoyaltyCustomers.FirstOrDefaultAsync(c => c.BrandId == brandId && c.PhoneNumber == identifier.Value);
        }

        private LoyaltyAuthResponseDto CreateAuthResponse(LoyaltyCustomer customer)
        {
            return new LoyaltyAuthResponseDto
            {
                AccessToken = GenerateJwt(customer),
                CustomerId = customer.Id,
                CustomerType = customer.CustomerType.ToString(),
                StudentVerificationStatus = customer.StudentVerificationStatus.ToString(),
                TotalPoints = customer.TotalPoints
            };
        }

        private string GenerateJwt(LoyaltyCustomer customer)
        {
            var displayName = customer.FullName ?? customer.Email ?? customer.PhoneNumber ?? customer.Id.ToString();
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, customer.Id.ToString()),
                new("LoyaltyCustomerId", customer.Id.ToString()),
                new("BrandId", customer.BrandId.ToString()),
                new(ClaimTypes.Role, "LoyaltyCustomer"),
                new(ClaimTypes.Name, displayName)
            };

            var key = _configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ApiException("Thiếu cấu hình JWT key.", 500);
            }

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);

            var expireMinutes = GetPositiveConfiguration("Jwt:ExpireMinutes", 60);
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private int GetPositiveConfiguration(string key, int defaultValue)
        {
            return int.TryParse(_configuration[key], out var value) && value > 0 ? value : defaultValue;
        }

        private static string NormalizePurpose(string purpose)
        {
            return purpose.Equals("Register", StringComparison.OrdinalIgnoreCase) ? "Register" : "Login";
        }

        private static NormalizedIdentifier NormalizeIdentifier(string rawIdentifier)
        {
            var identifier = rawIdentifier.Trim();
            if (identifier.Contains('@'))
            {
                if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(identifier))
                {
                    throw new ApiException("Email không hợp lệ.", 400);
                }

                return new NormalizedIdentifier(identifier.ToLowerInvariant(), "Email");
            }

            var phoneNumber = Regex.Replace(identifier, "[^0-9+]", string.Empty);
            if (phoneNumber.StartsWith("+84"))
            {
                phoneNumber = $"0{phoneNumber[3..]}";
            }
            else if (phoneNumber.StartsWith("84") && phoneNumber.Length == 11)
            {
                phoneNumber = $"0{phoneNumber[2..]}";
            }

            if (!Regex.IsMatch(phoneNumber, "^0\\d{9}$"))
            {
                throw new ApiException("Số điện thoại không hợp lệ.", 400);
            }

            return new NormalizedIdentifier(phoneNumber, "Phone");
        }

        private static bool IsIdentifierVerified(LoyaltyCustomer customer, string channel)
        {
            return channel == "Email" ? customer.IsEmailVerified : customer.IsPhoneNumberVerified;
        }

        private sealed record NormalizedIdentifier(string Value, string Channel);
    }
}
