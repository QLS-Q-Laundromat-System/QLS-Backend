using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QLS.Backend.DTOs.Brand;

namespace QLS.Backend.Interfaces.Brand
{
    public interface IPaymentConfigService
    {
        Task<PaymentConfigResponseDto> CreateConfigAsync(CreatePaymentConfigDto dto);
        Task<PaymentConfigResponseDto> UpdateConfigAsync(Guid id, UpdatePaymentConfigDto dto);
        Task<IEnumerable<PaymentConfigResponseDto>> GetConfigsByBrandAsync(Guid brandId);
        Task<PaymentConfigResponseDto> GetConfigByIdAsync(Guid id);
        Task<bool> DeleteConfigAsync(Guid id);
        Task<bool> ActivateConfigAsync(Guid id);
        Task<PaymentProviderInstructionsDto> GetInstructionsAsync(string provider, string baseUrl);
        Task<bool> VerifyConfigAsync(Guid id);
    }
}
