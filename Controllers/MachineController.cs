using Microsoft.AspNetCore.Mvc;
using QLS.Backend.Services;
using QLS.Backend.DTOs;
using QLS.Backend.Integrations.LG;

namespace QLS.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MachineController : ControllerBase
{
    private readonly IMachineDetailService _machineDetailService;

    // Chỉ Inject duy nhất Service mà Sơn đang có
    public MachineController(IMachineDetailService machineDetailService)
    {
        _machineDetailService = machineDetailService;
    }

    // API lấy trạng thái trực tiếp từ LG
    [HttpGet("status/{storeId}")]
    public async Task<IActionResult> GetLgStatus(Guid storeId)
    {
        var result = await _machineDetailService.GetLgMachineStatusAsync(storeId);
        return Ok(result);
    }

}