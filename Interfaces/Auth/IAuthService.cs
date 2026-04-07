using QLS.Backend.DTOs;
using QLS.Backend.Models.Enums;
using System.Threading.Tasks;

namespace QLS.Backend.Interfaces.Auth
{
    public interface IAuthService
    {
        Task<string?> LoginAsync(LoginRequest request);
        Task<bool> RegisterAsync(RegisterRequest request);
        Task<bool> CreateAdminAccountAsync(CreateAccountRequest request, UserRole creatorRole, Guid? creatorBrandId);
    }
}
