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

        public ZaloAuthService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<ZaloLoginResponseDto> LoginAsync(ZaloLoginRequestDto request)
        {
            var brandExists = await _context.Brands.AnyAsync(b => b.Id == request.BrandId && b.IsActive);
            if (!brandExists)
            {
                throw new ApiException("Brand không tồn tại hoặc đã bị khóa.", 404);
            }

            var now = DateTime.UtcNow;
            var customer = await _context.LoyaltyCustomers
                .FirstOrDefaultAsync(c => c.BrandId == request.BrandId && c.ZaloUserId == request.ZaloUserId);

            if (customer == null)
            {
                customer = new LoyaltyCustomer
                {
                    BrandId = request.BrandId,
                    ZaloUserId = request.ZaloUserId,
                    ZaloOAUserId = request.ZaloOAUserId,
                    FullName = request.FullName,
                    AvatarUrl = request.AvatarUrl,
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
                customer.ZaloOAUserId = request.ZaloOAUserId;
                customer.FullName = request.FullName;
                customer.AvatarUrl = request.AvatarUrl;
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
                new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString()),
                new Claim("LoyaltyCustomerId", customer.Id.ToString()),
                new Claim("BrandId", customer.BrandId.ToString()),
                new Claim(ClaimTypes.Role, "LoyaltyCustomer"),
                new Claim(ClaimTypes.Name, customer.FullName ?? customer.ZaloUserId)
            };

            var key = _configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ApiException("Thiếu cấu hình JWT key.", 500);
            }

            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);

            var expireMinutes = Convert.ToInt32(_configuration["Jwt:ExpireMinutes"] ?? "60");

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
