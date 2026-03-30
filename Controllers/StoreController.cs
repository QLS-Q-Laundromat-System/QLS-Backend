using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.Models;

namespace QLS.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoreController : ControllerBase
{
    private readonly AppDbContext _context;

    public StoreController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Store>>> GetStores()
    {
        try
        {
            var stores = await _context.Stores.ToListAsync();
            return Ok(stores);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi lấy dữ liệu: " + ex.Message });
        }
    }

    // API mới: Đếm xem có bao nhiêu Store
    [HttpGet("count")]
    public async Task<ActionResult<int>> GetStoreCount()
    {
        try
        {
            var count = await _context.Stores.CountAsync();
            return Ok(new { success = true, count = count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi đếm: " + ex.Message });
        }
    }
}
