using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs.Owner;
using QLS.Backend.Interfaces.Owner;
using System.Threading.Tasks;

namespace QLS.Backend.Controllers.Owner
{
    [Route("api/[controller]")]
    [ApiController]
    // BẮT BUỘC: Chỉ những người có Token hợp lệ VÀ có Role là "SuperAdmin" mới được vào
    [Authorize(Roles = "SuperAdmin")] 
    public class OwnerController : ControllerBase
    {
        private readonly IOwnerService _ownerService;

        public OwnerController(IOwnerService ownerService)
        {
            _ownerService = ownerService;
        }

        // 1. API Lấy danh sách toàn bộ Chủ chuỗi
        [HttpGet]
        public async Task<IActionResult> GetAllOwners()
        {
            var owners = await _ownerService.GetAllOwnersAsync();
            return Ok(owners);
        }

        // 2. API Tạo mới một Chủ chuỗi
        [HttpPost]
        public async Task<IActionResult> CreateOwner([FromBody] CreateOwnerDto dto)
        {
            var newOwner = await _ownerService.CreateOwnerAsync(dto);
            return Ok(new 
            {
                message = "Tạo chủ chuỗi thành công",
                data = newOwner
            });
        }
    }
}
