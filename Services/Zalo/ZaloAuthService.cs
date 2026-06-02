using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QLS.Backend.Data;
using QLS.Backend.DTOs.Zalo;
using QLS.Backend.Exceptions;
using QLS.Backend.Interfaces.Zalo;
using QLS.Backend.Models;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Services.Zalo
{
    public class ZaloAuthService : IZaloAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IZaloGraphApiClient _zaloGraphApiClient;

        public ZaloAuthService(
            AppDbContext context,
            IConfiguration configuration,
            IZaloGraphApiClient zaloGraphApiClient)
        {
            _context = context;
            _configuration = configuration;
            _zaloGraphApiClient = zaloGraphApiClient;
        }

        public async Task<ZaloLoginResponseDto> LoginAsync(ZaloLoginRequestDto request)
        {
            var brandExists = await _context.Brands.AnyAsync(b => b.Id == request.BrandId && b.IsActive);
            if (!brandExists)
            {
                throw new ApiException("Brand không tồn tại hoặc đã bị khóa.", 404);
            }

            var profile = await _zaloGraphApiClient.GetProfileAsync(request.AccessToken.Trim());
            var now = DateTime.UtcNow;
            var customer = await _context.LoyaltyCustomers
                .FirstOrDefaultAsync(c => c.BrandId == request.BrandId && c.ZaloUserId == profile.Id);

            if (customer == null)
            {
                customer = new LoyaltyCustomer
                {
                    BrandId = request.BrandId,
                    ZaloUserId = profile.Id,
                    FullName = profile.Name,
                    AvatarUrl = profile.AvatarUrl,
                    CustomerType = CustomerType.Member,
                    StudentVerificationStatus = StudentVerificationStatus.None,
                    TotalPoints = 0,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _context.LoyaltyCustomers.Add(customer);
            }
            else
            {
                customer.FullName = profile.Name;
                customer.AvatarUrl = profile.AvatarUrl;
                customer.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();

            return new ZaloLoginResponseDto
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
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, customer.Id.ToString()),
                new("LoyaltyCustomerId", customer.Id.ToString()),
                new("BrandId", customer.BrandId.ToString()),
                new(ClaimTypes.Role, "LoyaltyCustomer"),
                new(ClaimTypes.Name, customer.FullName ?? customer.ZaloUserId)
            };

            var key = _configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ApiException("Thiếu cấu hình JWT key.", 500);
            }

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);

            var expireMinutes = Convert.ToInt32(_configuration["Jwt:ExpireMinutes"] ?? "60");
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
