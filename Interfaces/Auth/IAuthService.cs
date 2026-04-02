using QLS.Backend.DTOs;
using System.Threading.Tasks;

namespace QLS.Backend.Interfaces.Auth
{
    public interface IAuthService
    {
        Task<string?> LoginAsync(LoginRequest request);
    }
}
