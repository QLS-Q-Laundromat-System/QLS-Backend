using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QLS.Backend.DTOs.DiscountCode;

namespace QLS.Backend.Interfaces.DiscountCode
{
    public interface IDiscountCodeService
    {
        Task<DiscountCodeResponseDto> CreateAsync(Guid brandId, DiscountCodeCreateDto dto);
        Task<DiscountCodeResponseDto> UpdateAsync(Guid brandId, Guid id, DiscountCodeUpdateDto dto);
        Task<DiscountCodeResponseDto> GetByIdAsync(Guid brandId, Guid id);
        Task<IEnumerable<DiscountCodeResponseDto>> GetAllByBrandAsync(Guid brandId);
        Task<ValidateDiscountResponseDto> ValidateCodeAsync(Guid userId, ValidateDiscountRequestDto request);
        Task<bool> RecordUsageAsync(Guid discountCodeId, Guid userId, Guid machineSessionId);
        Task<DiscountCodeOverviewDto> GetOverviewAsync(Guid brandId);
        Task<IEnumerable<DiscountCodeUsageDto>> GetUsageHistoryAsync(Guid brandId, Guid discountCodeId);
    }
}
