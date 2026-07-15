using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.DTOs;
using QLS.Backend.DTOs.Notification;
using System.Security.Claims;

namespace QLS.Backend.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(Roles = "Manager,Staff")]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public NotificationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] bool unreadOnly = true,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(User.FindFirstValue("StoreId"), out var storeId))
            return Unauthorized(ApiResponse<object>.Error(401, "Tài khoản chưa được gán cửa hàng."));

        limit = Math.Clamp(limit, 1, 50);
        var query = _context.MachineNotifications
            .AsNoTracking()
            .Where(n => n.StoreId == storeId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .Select(n => new MachineNotificationDto
            {
                Id = n.Id,
                MachineId = n.MachineId,
                SessionId = n.SessionId,
                MachineName = n.Machine!.Name,
                MachineType = n.Machine.Type.ToString(),
                Type = n.Type,
                Message = n.Message,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IEnumerable<MachineNotificationDto>>.Success(notifications));
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(User.FindFirstValue("StoreId"), out var storeId))
            return Unauthorized(ApiResponse<object>.Error(401, "Tài khoản chưa được gán cửa hàng."));

        var notification = await _context.MachineNotifications
            .FirstOrDefaultAsync(n => n.Id == id && n.StoreId == storeId, cancellationToken);
        if (notification == null)
            return NotFound(ApiResponse<object>.Error(404, "Không tìm thấy thông báo."));

        notification.IsRead = true;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Success(new { notification.Id }));
    }
}
