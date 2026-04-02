using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QLS.Backend.Data;
using QLS.Backend.DTOs;
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
            // Tìm user trong DB
            var user = await _context.UserAdmins.FirstOrDefaultAsync(u => u.Email == request.Email);

            // Kiểm tra mật khẩu
            if (user == null || user.PasswordHash != request.Password)
            {
                return null; // Trả về null báo hiệu thất bại
            }

            // Tạo Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("FullName", user.FullName)
            };

            if (user.OwnerId != null)
            {
                claims.Add(new Claim("OwnerId", user.OwnerId.ToString()!));
            }

            if (user.BranchId != null)
            {
                claims.Add(new Claim("BranchId", user.BranchId.ToString()!));
            }

            // Ký Token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(Convert.ToDouble(_config["Jwt:ExpireDays"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}