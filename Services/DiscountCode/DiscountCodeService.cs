using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.DTOs.DiscountCode;
using QLS.Backend.Exceptions;
using QLS.Backend.Interfaces.DiscountCode;
using QLS.Backend.Models;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Services.DiscountCode
{
    public class DiscountCodeService : IDiscountCodeService
    {
        private readonly AppDbContext _context;

        public DiscountCodeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DiscountCodeResponseDto> CreateAsync(Guid brandId, DiscountCodeCreateDto dto)
        {
            var existing = await _context.DiscountCodes
                .AnyAsync(dc => dc.BrandId == brandId && dc.Code.ToLower() == dto.Code.ToLower());

            if (existing)
            {
                throw new ApiException("Mã giảm giá đã tồn tại trong chuỗi cửa hàng này.", 400);
            }

            var discountCode = new Models.DiscountCode
            {
                BrandId = brandId,
                Code = dto.Code.ToUpper(),
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                MaxDiscountAmount = dto.MaxDiscountAmount,
                MinOrderValue = dto.MinOrderValue,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                UsageLimit = dto.UsageLimit,
                UserUsageLimit = dto.UserUsageLimit,
                IsActive = dto.IsActive,
                Description = dto.Description,
                IsApplyAllStores = dto.IsApplyAllStores
            };

            _context.DiscountCodes.Add(discountCode);

            if (!dto.IsApplyAllStores && dto.StoreIds != null && dto.StoreIds.Any())
            {
                foreach (var storeId in dto.StoreIds)
                {
                    _context.DiscountCodeStores.Add(new DiscountCodeStore
                    {
                        DiscountCodeId = discountCode.Id,
                        StoreId = storeId
                    });
                }
            }

            await _context.SaveChangesAsync();

            return await GetByIdAsync(brandId, discountCode.Id);
        }

        public async Task<DiscountCodeResponseDto> UpdateAsync(Guid brandId, Guid id, DiscountCodeUpdateDto dto)
        {
            var discountCode = await _context.DiscountCodes
                .FirstOrDefaultAsync(dc => dc.Id == id && dc.BrandId == brandId);

            if (discountCode == null)
            {
                throw new ApiException("Không tìm thấy mã giảm giá.", 404);
            }

            if (dto.DiscountType.HasValue) discountCode.DiscountType = dto.DiscountType.Value;
            if (dto.DiscountValue.HasValue) discountCode.DiscountValue = dto.DiscountValue.Value;
            if (dto.MaxDiscountAmount.HasValue) discountCode.MaxDiscountAmount = dto.MaxDiscountAmount.Value;
            if (dto.MinOrderValue.HasValue) discountCode.MinOrderValue = dto.MinOrderValue.Value;
            if (dto.StartDate.HasValue) discountCode.StartDate = dto.StartDate.Value;
            if (dto.EndDate.HasValue) discountCode.EndDate = dto.EndDate.Value;
            if (dto.UsageLimit.HasValue) discountCode.UsageLimit = dto.UsageLimit.Value;
            if (dto.UserUsageLimit.HasValue) discountCode.UserUsageLimit = dto.UserUsageLimit.Value;
            if (dto.IsActive.HasValue) discountCode.IsActive = dto.IsActive.Value;
            if (dto.Description != null) discountCode.Description = dto.Description;

            if (dto.IsApplyAllStores.HasValue)
            {
                discountCode.IsApplyAllStores = dto.IsApplyAllStores.Value;
                
                var existingStores = await _context.DiscountCodeStores
                    .Where(ds => ds.DiscountCodeId == id).ToListAsync();
                _context.DiscountCodeStores.RemoveRange(existingStores);

                if (!discountCode.IsApplyAllStores && dto.StoreIds != null && dto.StoreIds.Any())
                {
                    foreach (var storeId in dto.StoreIds)
                    {
                        _context.DiscountCodeStores.Add(new DiscountCodeStore
                        {
                            DiscountCodeId = discountCode.Id,
                            StoreId = storeId
                        });
                    }
                }
            }
            else if (!discountCode.IsApplyAllStores && dto.StoreIds != null)
            {
                var existingStores = await _context.DiscountCodeStores
                    .Where(ds => ds.DiscountCodeId == id).ToListAsync();
                _context.DiscountCodeStores.RemoveRange(existingStores);
                
                foreach (var storeId in dto.StoreIds)
                {
                    _context.DiscountCodeStores.Add(new DiscountCodeStore
                    {
                        DiscountCodeId = discountCode.Id,
                        StoreId = storeId
                    });
                }
            }

            await _context.SaveChangesAsync();

            return await GetByIdAsync(brandId, discountCode.Id);
        }

        public async Task<IEnumerable<DiscountCodeResponseDto>> GetAllByBrandAsync(Guid brandId)
        {
            var codes = await _context.DiscountCodes
                .Include(dc => dc.DiscountCodeStores)
                .Where(dc => dc.BrandId == brandId)
                .OrderByDescending(dc => dc.CreatedAt)
                .ToListAsync();

            return codes.Select(MapToResponseDto);
        }

        public async Task<DiscountCodeResponseDto> GetByIdAsync(Guid brandId, Guid id)
        {
            var code = await _context.DiscountCodes
                .Include(dc => dc.DiscountCodeStores)
                .FirstOrDefaultAsync(dc => dc.Id == id && dc.BrandId == brandId);

            if (code == null)
            {
                throw new ApiException("Không tìm thấy mã giảm giá.", 404);
            }

            return MapToResponseDto(code);
        }

        public async Task<ValidateDiscountResponseDto> ValidateCodeAsync(Guid userId, ValidateDiscountRequestDto request)
        {
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == request.StoreId);
            if (store == null) return new ValidateDiscountResponseDto { IsValid = false, Message = "Cửa hàng không tồn tại." };

            var code = await _context.DiscountCodes
                .Include(dc => dc.DiscountCodeStores)
                .FirstOrDefaultAsync(dc => dc.BrandId == store.BrandId && dc.Code.ToLower() == request.Code.ToLower());

            if (code == null || !code.IsActive)
                return new ValidateDiscountResponseDto { IsValid = false, Message = "Mã giảm giá không tồn tại hoặc đã bị vô hiệu hóa." };

            if (DateTime.UtcNow < code.StartDate)
                return new ValidateDiscountResponseDto { IsValid = false, Message = "Mã giảm giá chưa đến ngày sử dụng." };

            if (DateTime.UtcNow > code.EndDate)
                return new ValidateDiscountResponseDto { IsValid = false, Message = "Mã giảm giá đã hết hạn." };

            if (code.UsageLimit.HasValue && code.UsedCount >= code.UsageLimit.Value)
                return new ValidateDiscountResponseDto { IsValid = false, Message = "Mã giảm giá đã hết lượt sử dụng trên hệ thống." };

            if (code.MinOrderValue.HasValue && request.OrderTotal < code.MinOrderValue.Value)
                return new ValidateDiscountResponseDto { IsValid = false, Message = $"Đơn hàng phải tối thiểu {code.MinOrderValue.Value:N0}đ để áp dụng mã này." };

            if (!code.IsApplyAllStores && !code.DiscountCodeStores.Any(ds => ds.StoreId == request.StoreId))
                return new ValidateDiscountResponseDto { IsValid = false, Message = "Mã giảm giá không áp dụng tại cơ sở này." };

            if (code.UserUsageLimit.HasValue)
            {
                var userUsageCount = await _context.DiscountCodeUsages
                    .CountAsync(du => du.DiscountCodeId == code.Id && du.UserId == userId);
                if (userUsageCount >= code.UserUsageLimit.Value)
                    return new ValidateDiscountResponseDto { IsValid = false, Message = "Bạn đã hết lượt sử dụng mã này." };
            }

            // Calculate discount amount
            decimal discountAmount = 0;
            if (code.DiscountType == DiscountType.Percentage)
            {
                discountAmount = request.OrderTotal * (code.DiscountValue / 100m);
                if (code.MaxDiscountAmount.HasValue && discountAmount > code.MaxDiscountAmount.Value)
                {
                    discountAmount = code.MaxDiscountAmount.Value;
                }
            }
            else
            {
                discountAmount = code.DiscountValue;
            }

            // Ensure discount doesn't exceed order total
            if (discountAmount > request.OrderTotal)
            {
                discountAmount = request.OrderTotal;
            }

            return new ValidateDiscountResponseDto
            {
                IsValid = true,
                Message = "Mã giảm giá hợp lệ.",
                DiscountAmount = discountAmount,
                DiscountCodeId = code.Id
            };
        }

        public async Task<bool> RecordUsageAsync(Guid discountCodeId, Guid userId, Guid machineSessionId)
        {
            var code = await _context.DiscountCodes.FirstOrDefaultAsync(c => c.Id == discountCodeId);
            if (code == null) return false;

            // 1. Tăng số lượt sử dụng
            code.UsedCount += 1;

            // 2. Ghi lại lịch sử
            var usage = new DiscountCodeUsage
            {
                DiscountCodeId = discountCodeId,
                UserId = userId,
                MachineSessionId = machineSessionId,
                UsedAt = DateTime.UtcNow
            };

            _context.DiscountCodeUsages.Add(usage);
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<DiscountCodeOverviewDto> GetOverviewAsync(Guid brandId)
        {
            var now = DateTime.UtcNow;
            var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var activeCodesCount = await _context.DiscountCodes
                .Where(dc => dc.BrandId == brandId && dc.IsActive && dc.EndDate > now)
                .CountAsync();

            var usageThisMonthQuery = _context.DiscountCodeUsages
                .Include(u => u.DiscountCode)
                .Include(u => u.MachineSession)
                .Where(u => u.DiscountCode!.BrandId == brandId && u.UsedAt >= currentMonthStart);

            var totalUsages = await usageThisMonthQuery.CountAsync();

            // Lấy tổng discount amount của tháng này từ MachineSession.
            // Để đơn giản, giả sử ở đây nếu session có PriceListId / hoặc ta có cột DiscountAmount. 
            // Nếu không lưu trực tiếp cột DiscountAmount ở MachineSession, ta ước tính thông qua PricePaid vs Total (cần custom)
            // Hiện tại, ta sẽ tính tổng revenue từ các Session có giảm giá.
            var sessionsThisMonth = await usageThisMonthQuery
                .Select(u => u.MachineSession)
                .Where(ms => ms != null)
                .ToListAsync();

            decimal totalRevenue = sessionsThisMonth.Sum(ms => ms!.PricePaid);
            // Giả định nếu ta thêm cột DiscountAmount vào MachineSession thì có thể Sum.
            // Nếu không, ta set tạm = 0 hoặc lấy từ bảng khác.
            decimal totalDiscount = 0; // Sẽ cần cập nhật logic thực tế sau nếu DB có lưu số tiền giảm chi tiết.

            return new DiscountCodeOverviewDto
            {
                TotalActiveCodes = activeCodesCount,
                TotalUsagesThisMonth = totalUsages,
                TotalDiscountAmountThisMonth = totalDiscount,
                TotalRevenueFromDiscountedOrdersThisMonth = totalRevenue
            };
        }

        public async Task<IEnumerable<DiscountCodeUsageDto>> GetUsageHistoryAsync(Guid brandId, Guid discountCodeId)
        {
            var code = await _context.DiscountCodes.FirstOrDefaultAsync(dc => dc.Id == discountCodeId && dc.BrandId == brandId);
            if (code == null) throw new ApiException("Không tìm thấy mã giảm giá.", 404);

            var usages = await _context.DiscountCodeUsages
                .Include(u => u.User)
                .Where(u => u.DiscountCodeId == discountCodeId)
                .OrderByDescending(u => u.UsedAt)
                .ToListAsync();

            return usages.Select(u => new DiscountCodeUsageDto
            {
                Id = u.Id,
                UserId = u.UserId,
                UserFullName = u.User?.FullName ?? "Unknown",
                UserPhoneOrEmail = string.IsNullOrEmpty(u.User?.Email) ? "No Data" : u.User.Email,
                MachineSessionId = u.MachineSessionId,
                UsedAt = u.UsedAt
            });
        }

        private DiscountCodeResponseDto MapToResponseDto(Models.DiscountCode code)
        {
            return new DiscountCodeResponseDto
            {
                Id = code.Id,
                BrandId = code.BrandId,
                Code = code.Code,
                DiscountType = code.DiscountType,
                DiscountValue = code.DiscountValue,
                MaxDiscountAmount = code.MaxDiscountAmount,
                MinOrderValue = code.MinOrderValue,
                StartDate = code.StartDate,
                EndDate = code.EndDate,
                UsageLimit = code.UsageLimit,
                UsedCount = code.UsedCount,
                UserUsageLimit = code.UserUsageLimit,
                IsActive = code.IsActive,
                Description = code.Description,
                IsApplyAllStores = code.IsApplyAllStores,
                CreatedAt = code.CreatedAt,
                StoreIds = code.DiscountCodeStores?.Select(s => s.StoreId).ToList() ?? new List<Guid>()
            };
        }
    }
}
