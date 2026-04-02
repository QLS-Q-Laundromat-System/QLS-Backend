using QLS.Backend.DTOs.Owner;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QLS.Backend.Interfaces.Owner
{
    public interface IOwnerService
    {
        Task<List<OwnerResponseDto>> GetAllOwnersAsync();
        Task<OwnerResponseDto> CreateOwnerAsync(CreateOwnerDto dto);
    }
}
