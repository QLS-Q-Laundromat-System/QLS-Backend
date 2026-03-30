using Microsoft.AspNetCore.Mvc;
using QLS.Backend.Interfaces;
using QLS.Backend.DTOs;

namespace QLS.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WasherController : ControllerBase
{
    private readonly IWasherService _washerService;

    public WasherController(IWasherService washerService)
    {
        _washerService = washerService;
    }

    [HttpGet("status/{storeId}")]
    public async Task<ActionResult<IEnumerable<WasherStatusDto>>> GetStatus(string storeId)
    {
        try
        {
            var result = await _washerService.GetWasherStatusAsync(storeId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi lấy dữ liệu: " + ex.Message });
        }
    }
}
