using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QLS.Backend.Data;
using QLS.Backend.DTOs;
using QLS.Backend.Models;
using QLS.Backend.Models.Enums;
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

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            // 1. Tìm Account trong DB thông qua Username
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Username == request.Username);

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
            var expireMinutes = Convert.ToInt32(_config["Jwt:ExpireMinutes"] ?? "60"); // default 60m

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: creds
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            // 6. Trả về DTO hoàn chỉnh
            return new LoginResponse
            {
                Tokens = new TokenDto
                {
                    AccessToken = accessToken,
                    RefreshToken = "def5020054...", // Placeholder cho RefreshToken
                    ExpiresIn = expireMinutes * 60
                },
                User = new UserDto
                {
                    Id = account.Id.ToString(),
                    Username = account.Username,
                    FullName = profile?.FullName ?? account.Username,
                    Role = account.Role.ToString(),
                    BrandId = account.BrandId?.ToString(),
                    Avatar = "https://ui-avatars.com/api/?name=" + (profile?.FullName ?? account.Username) // Placeholder avatar
                }
            };
        }

        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            // Check if user exists
            if (await _context.Accounts.AnyAsync(a => a.Username == request.Username))
            {
                return false;
            }

            var userId = Guid.NewGuid();

            // Create Account
            var account = new Account
            {
                Id = userId,
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = Models.Enums.UserRole.Customer,
                IsActive = true
            };

            // Create User Profile
            var user = new User
            {
                Id = userId,
                FullName = request.FullName,
                Email = request.Email,
                IsActive = true
            };

            _context.Accounts.Add(account);
            _context.Users.Add(user);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CreateAdminAccountAsync(CreateAccountRequest request, UserRole creatorRole, Guid? creatorBrandId)
        {
            // 1. Kiểm tra quyên hạn (Hierarchy Check)
            if (creatorRole == UserRole.SystemAdmin)
            {
                // SystemAdmin chỉ được tạo BrandAdmin
                if (request.Role != UserRole.BrandAdmin) return false;
            }
            else if (creatorRole == UserRole.BrandAdmin)
            {
                // BrandAdmin chỉ được tạo Manager hoặc Staff trong chuỗi của mình
                if (request.Role != UserRole.Manager && request.Role != UserRole.Staff) return false;
                
                // Tự động gán BrandId của người tạo cho tài khoản mới
                request.BrandId = creatorBrandId; 
            }
            else
            {
                // Các role khác không có quyên tạo tài khoản theo cách này
                return false;
            }

            // 2. Kiểm tra tài khoản tồn tại
            if (await _context.Accounts.AnyAsync(a => a.Username == request.Username))
            {
                return false;
            }

            var userId = Guid.NewGuid();

            var account = new Account
            {
                Id = userId,
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role,
                BrandId = request.BrandId,
                StoreId = request.StoreId,
                IsActive = true
            };

            var user = new User
            {
                Id = userId,
                FullName = request.FullName,
                Email = request.Email,
                IsActive = true
            };

            _context.Accounts.Add(account);
            _context.Users.Add(user);

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
