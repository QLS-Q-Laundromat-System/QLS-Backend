using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.DTOs.Brand;
using QLS.Backend.Interfaces.Brand;
using QLS.Backend.Models;

using System.Net.Http;
using System.Net.Http.Headers;

namespace QLS.Backend.Services.Brand
{
    public class PaymentConfigService : IPaymentConfigService
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public PaymentConfigService(AppDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<PaymentConfigResponseDto> CreateConfigAsync(CreatePaymentConfigDto dto)
        {
            var brandExists = await _context.Brands.AnyAsync(b => b.Id == dto.BrandId);
            if (!brandExists) throw new Exception("Thương hiệu không tồn tại.");

            if (dto.Provider?.ToUpper() == "SEPAY")
            {
                var isValid = await VerifySePayCredentialsAsync(dto.ApiKey);
                if (!isValid) throw new Exception("API Key không hợp lệ hoặc không thể kết nối đến SePay. Vui lòng kiểm tra lại thông tin.");
            }

            if (dto.IsActive)
            {
                var existingConfigs = await _context.PaymentConfigs
                    .Where(p => p.BrandId == dto.BrandId && p.Provider == dto.Provider && p.IsActive)
                    .ToListAsync();

                foreach (var config in existingConfigs)
                {
                    config.IsActive = false;
                }
            }

            var newConfig = new PaymentConfig
            {
                Id = Guid.NewGuid(),
                BrandId = dto.BrandId,
                Provider = dto.Provider,
                BankCode = dto.BankCode,
                AccountNumber = dto.AccountNumber,
                AccountName = dto.AccountName,
                ApiKey = dto.ApiKey,
                SecretKey = dto.SecretKey,
                IsActive = dto.IsActive,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PaymentConfigs.Add(newConfig);
            await _context.SaveChangesAsync();

            return MapToDto(newConfig);
        }

        public async Task<PaymentConfigResponseDto> UpdateConfigAsync(Guid id, UpdatePaymentConfigDto dto)
        {
            var config = await _context.PaymentConfigs.FindAsync(id);
            if (config == null) throw new Exception("Không tìm thấy cấu hình thanh toán.");

            var newProvider = dto.Provider ?? config.Provider;
            var newApiKey = dto.ApiKey ?? config.ApiKey;

            if (newProvider?.ToUpper() == "SEPAY" && (dto.ApiKey != null || dto.Provider != null))
            {
                var isValid = await VerifySePayCredentialsAsync(newApiKey);
                if (!isValid) throw new Exception("API Key không hợp lệ hoặc không thể kết nối đến SePay. Vui lòng kiểm tra lại thông tin.");
            }

            config.Provider = newProvider;
            config.BankCode = dto.BankCode ?? config.BankCode;
            config.AccountNumber = dto.AccountNumber ?? config.AccountNumber;
            config.AccountName = dto.AccountName ?? config.AccountName;
            config.ApiKey = newApiKey;
            config.SecretKey = dto.SecretKey ?? config.SecretKey;
            
            if (dto.IsActive != config.IsActive)
            {
                if (dto.IsActive)
                {
                    var existingConfigs = await _context.PaymentConfigs
                        .Where(p => p.BrandId == config.BrandId && p.Provider == config.Provider && p.IsActive && p.Id != id)
                        .ToListAsync();

                    foreach (var c in existingConfigs)
                    {
                        c.IsActive = false;
                    }
                }
                config.IsActive = dto.IsActive;
            }

            config.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapToDto(config);
        }

        public async Task<IEnumerable<PaymentConfigResponseDto>> GetConfigsByBrandAsync(Guid brandId)
        {
            var configs = await _context.PaymentConfigs
                .Where(p => p.BrandId == brandId)
                .OrderByDescending(p => p.IsActive)
                .ThenByDescending(p => p.UpdatedAt)
                .ToListAsync();

            return configs.Select(MapToDto);
        }

        public async Task<PaymentConfigResponseDto> GetConfigByIdAsync(Guid id)
        {
            var config = await _context.PaymentConfigs.FindAsync(id);
            if (config == null) throw new Exception("Không tìm thấy cấu hình thanh toán.");
            return MapToDto(config);
        }

        public async Task<bool> DeleteConfigAsync(Guid id)
        {
            var config = await _context.PaymentConfigs.FindAsync(id);
            if (config == null) return false;

            _context.PaymentConfigs.Remove(config);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ActivateConfigAsync(Guid id)
        {
            var config = await _context.PaymentConfigs.FindAsync(id);
            if (config == null) return false;

            var existingConfigs = await _context.PaymentConfigs
                .Where(p => p.BrandId == config.BrandId && p.Provider == config.Provider && p.IsActive && p.Id != id)
                .ToListAsync();

            foreach (var c in existingConfigs)
            {
                c.IsActive = false;
            }

            config.IsActive = true;
            config.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PaymentProviderInstructionsDto> GetInstructionsAsync(string provider, string baseUrl)
        {
            var webhookUrl = $"{baseUrl.TrimEnd('/')}/api/webhooks/{provider.ToLower()}";
            
            var dto = new PaymentProviderInstructionsDto
            {
                Provider = provider.ToUpper(),
                WebhookUrl = webhookUrl,
                Steps = new List<InstructionStepDto>()
            };

            if (provider.ToUpper() == "SEPAY")
            {
                dto.Steps.AddRange(new[]
                {
                    new InstructionStepDto { Order = 1, Title = "Đăng ký tài khoản", Description = "Truy cập https://sepay.vn và đăng ký tài khoản cho doanh nghiệp/cửa hàng của bạn." },
                    new InstructionStepDto { Order = 2, Title = "Liên kết ngân hàng", Description = "Trong dashboard SePay, thực hiện liên kết tài khoản ngân hàng bạn muốn nhận tiền." },
                    new InstructionStepDto { Order = 3, Title = "Lấy API Key", Description = "Vào mục Cấu hình API -> Tạo API Key mới. Sao chép API Key này để dán vào phần cấu hình của QLS." },
                    new InstructionStepDto { Order = 4, Title = "Cấu hình Webhook", Description = "Vào mục Webhooks -> Thêm Webhook mới. Sao chép và dán URL hiển thị bên dưới vào ô URL Webhook của SePay. Chọn loại sự kiện 'Giao dịch mới' (New Transaction)." },
                    new InstructionStepDto { Order = 5, Title = "Xác thực cấu hình", Description = "Sau khi điền đầy đủ API Key và Webhook Secret (nếu có), hãy nhấn nút 'Kiểm tra kết nối' để hoàn tất." }
                });
            }

            return dto;
        }

        public async Task<bool> VerifyConfigAsync(Guid id)
        {
            var config = await _context.PaymentConfigs.FindAsync(id);
            if (config == null) return false;

            if (config.Provider.ToUpper() == "SEPAY")
            {
                return await VerifySePayCredentialsAsync(config.ApiKey);
            }

            return true; // Mặc định true cho các provider chưa implement logic verify
        }

        private async Task<bool> VerifySePayCredentialsAsync(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey)) return false;

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                
                // 1. Thử endpoint v2 Production của SePay
                var response = await client.GetAsync("https://userapi.sepay.vn/v2/transactions?per_page=1");
                if (response.IsSuccessStatusCode) return true;

                // 2. Thử endpoint v2 Sandbox của SePay (Kiểm thử)
                var responseSandbox = await client.GetAsync("https://userapi-sandbox.sepay.vn/v2/transactions?per_page=1");
                if (responseSandbox.IsSuccessStatusCode) return true;

                // 3. Dự phòng: Thử endpoint v1 cũ
                var responseV1 = await client.GetAsync("https://my.sepay.vn/api/transactions/list?limit=1");
                return responseV1.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private PaymentConfigResponseDto MapToDto(PaymentConfig entity)
        {
            return new PaymentConfigResponseDto
            {
                Id = entity.Id,
                BrandId = entity.BrandId,
                Provider = entity.Provider,
                BankCode = entity.BankCode,
                AccountNumber = entity.AccountNumber,
                AccountName = entity.AccountName,
                ApiKey = entity.ApiKey,
                SecretKey = entity.SecretKey,
                IsActive = entity.IsActive,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
