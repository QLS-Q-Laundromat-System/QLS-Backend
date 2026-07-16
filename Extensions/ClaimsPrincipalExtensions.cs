using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.Exceptions;

namespace QLS.Backend.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool IsSystemAdmin(this ClaimsPrincipal user) =>
        user.IsInRole("SystemAdmin");

    public static Guid GetRequiredUserId(this ClaimsPrincipal user) =>
        GetRequiredGuidClaim(user, ClaimTypes.NameIdentifier);

    public static Guid GetRequiredStoreId(this ClaimsPrincipal user) =>
        GetRequiredGuidClaim(user, "StoreId");

    public static Guid GetRequiredBrandId(this ClaimsPrincipal user) =>
        GetRequiredGuidClaim(user, "BrandId");

    public static Task EnsureBrandAccessAsync(
        this ClaimsPrincipal user,
        Guid brandId)
    {
        if (user.IsSystemAdmin()) return Task.CompletedTask;

        if (!user.IsInRole("BrandAdmin") || user.GetRequiredBrandId() != brandId)
        {
            throw new ApiException("Bạn không có quyền truy cập dữ liệu của thương hiệu này.", 403);
        }

        return Task.CompletedTask;
    }

    public static async Task EnsureStoreAccessAsync(
        this ClaimsPrincipal user,
        AppDbContext context,
        Guid storeId)
    {
        if (user.IsSystemAdmin()) return;

        if (user.IsInRole("Manager") || user.IsInRole("Staff"))
        {
            if (user.GetRequiredStoreId() == storeId) return;
            throw new ApiException("Bạn không có quyền truy cập dữ liệu của cửa hàng này.", 403);
        }

        if (user.IsInRole("BrandAdmin"))
        {
            var brandId = user.GetRequiredBrandId();
            var belongsToBrand = await context.Stores
                .AsNoTracking()
                .AnyAsync(store => store.Id == storeId && store.BrandId == brandId);

            if (belongsToBrand) return;
        }

        throw new ApiException("Bạn không có quyền truy cập dữ liệu của cửa hàng này.", 403);
    }

    public static async Task EnsureMachineAccessAsync(
        this ClaimsPrincipal user,
        AppDbContext context,
        Guid machineId)
    {
        var storeId = await context.Machines
            .AsNoTracking()
            .Where(machine => machine.Id == machineId)
            .Select(machine => (Guid?)machine.StoreId)
            .FirstOrDefaultAsync();

        if (!storeId.HasValue)
        {
            throw new ApiException("Không tìm thấy máy.", 404);
        }

        await user.EnsureStoreAccessAsync(context, storeId.Value);
    }

    private static Guid GetRequiredGuidClaim(ClaimsPrincipal user, string claimType)
    {
        var value = user.FindFirst(claimType)?.Value;
        if (!Guid.TryParse(value, out var id))
        {
            throw new ApiException("Token không có thông tin định danh hợp lệ.", 401);
        }

        return id;
    }
}
