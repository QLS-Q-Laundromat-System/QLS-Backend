using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QLS.Backend.Data;
using QLS.Backend.DTOs;
using QLS.Backend.Models;
using QLS.Backend.Interfaces.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace QLS.Backend.Services
{
    // 2. Thực thi (Implementation)
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<string?> LoginAsync(LoginRequest request)
        {
            // 1. Tìm Account trong DB thông qua Username
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Username == request.Email);

            if (account == null || !account.IsActive)
            {
                return null;
            }

            // 2. Kiểm tra mật khẩu bằng BCrypt
            if (!BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
            {
                return null;
            }

            // 3. Tìm thông tin profile (User) nếu có để lấy FullName/Email
            var profile = await _context.Users.FindAsync(account.Id);

            // 4. Tạo Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new Claim(ClaimTypes.Name, account.Username),
                new Claim(ClaimTypes.Role, account.Role.ToString()),
                new Claim("FullName", profile?.FullName ?? account.Username)
            };

            if (account.BrandId != null)
            {
                claims.Add(new Claim("BrandId", account.BrandId.ToString()!));
            }

            if (account.StoreId != null)
            {
                claims.Add(new Claim("StoreId", account.StoreId.ToString()!));
            }

            // 5. Ký Token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(Convert.ToDouble(_config["Jwt:ExpireDays"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}