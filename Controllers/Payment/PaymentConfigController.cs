using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using QLS.Backend.Data;
using QLS.Backend.DTOs.Brand;
using QLS.Backend.Extensions;
using QLS.Backend.Interfaces.Payment;
using QLS.Backend.Services;
using System;
using System.Threading.Tasks;

namespace QLS.Backend.Controllers
{
    [ApiController]
    [Route("api/v1/payment-configs")]
    [Authorize(Roles = "SystemAdmin,BrandAdmin")]
    public class PaymentConfigController : ControllerBase
    {
        private readonly IPaymentConfigService _service;
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public PaymentConfigController(
            IPaymentConfigService service,
            AppDbContext context,
            IAuditLogService auditLogService)
        {
            _service = service;
            _context = context;
            _auditLogService = auditLogService;
        }

        [HttpGet("instructions/{provider}")]
        public async Task<IActionResult> GetInstructions(string provider)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _service.GetInstructionsAsync(provider, baseUrl);
            return Ok(new { success = true, data = result });
        }

        [HttpPost("brand/{brandId}")]
        public async Task<IActionResult> Create(Guid brandId, [FromBody] CreatePaymentConfigDto dto)
        {
            try
            {
                await User.EnsureBrandAccessAsync(brandId);
                dto.BrandId = brandId;
                var result = await _service.CreateConfigAsync(dto);
                await _auditLogService.LogAsync(
                    HttpContext,
                    "payment_config.create",
                    "PaymentConfig",
                    result.Id.ToString(),
                    success: true,
                    metadata: new
                    {
                        brandId,
                        dto.Provider,
                        dto.BankCode,
                        AccountNumber = MaskAccountNumber(dto.AccountNumber),
                        dto.IsActive,
                        HasApiKey = !string.IsNullOrWhiteSpace(dto.ApiKey),
                        HasSecretKey = !string.IsNullOrWhiteSpace(dto.SecretKey)
                    });
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                await _auditLogService.LogAsync(
                    HttpContext,
                    "payment_config.create",
                    "PaymentConfig",
                    brandId.ToString(),
                    success: false,
                    failureReason: ex.Message,
                    metadata: new
                    {
                        brandId,
                        dto.Provider,
                        dto.BankCode,
                        AccountNumber = MaskAccountNumber(dto.AccountNumber),
                        dto.IsActive,
                        HasApiKey = !string.IsNullOrWhiteSpace(dto.ApiKey),
                        HasSecretKey = !string.IsNullOrWhiteSpace(dto.SecretKey)
                    });
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("brand/{brandId}")]
        public async Task<IActionResult> GetByBrand(Guid brandId)
        {
            await User.EnsureBrandAccessAsync(brandId);
            var result = await _service.GetConfigsByBrandAsync(brandId);
            return Ok(new { success = true, data = result });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                await EnsureConfigAccessAsync(id);
                var result = await _service.GetConfigByIdAsync(id);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePaymentConfigDto dto)
        {
            try
            {
                await EnsureConfigAccessAsync(id);
                var result = await _service.UpdateConfigAsync(id, dto);
                await _auditLogService.LogAsync(
                    HttpContext,
                    "payment_config.update",
                    "PaymentConfig",
                    id.ToString(),
                    success: true,
                    metadata: new
                    {
                        dto.Provider,
                        dto.BankCode,
                        AccountNumber = MaskAccountNumber(dto.AccountNumber),
                        dto.IsActive,
                        HasApiKey = !string.IsNullOrWhiteSpace(dto.ApiKey),
                        HasSecretKey = !string.IsNullOrWhiteSpace(dto.SecretKey)
                    });
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                await _auditLogService.LogAsync(
                    HttpContext,
                    "payment_config.update",
                    "PaymentConfig",
                    id.ToString(),
                    success: false,
                    failureReason: ex.Message,
                    metadata: new
                    {
                        dto.Provider,
                        dto.BankCode,
                        AccountNumber = MaskAccountNumber(dto.AccountNumber),
                        dto.IsActive,
                        HasApiKey = !string.IsNullOrWhiteSpace(dto.ApiKey),
                        HasSecretKey = !string.IsNullOrWhiteSpace(dto.SecretKey)
                    });
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPatch("{id}/activate")]
        public async Task<IActionResult> Activate(Guid id)
        {
            await EnsureConfigAccessAsync(id);
            var result = await _service.ActivateConfigAsync(id);
            if (!result) return NotFound(new { success = false, message = "Không tìm thấy cấu hình." });
            await _auditLogService.LogAsync(
                HttpContext,
                "payment_config.activate",
                "PaymentConfig",
                id.ToString(),
                success: true);
            return Ok(new { success = true, message = "Đã kích hoạt cấu hình thành công." });
        }

        [HttpPost("{id}/verify")]
        public async Task<IActionResult> Verify(Guid id)
        {
            await EnsureConfigAccessAsync(id);
            var result = await _service.VerifyConfigAsync(id);
            if (!result) return BadRequest(new { success = false, message = "Xác thực cấu hình thất bại. Vui lòng kiểm tra lại API Key hoặc cấu hình." });
            return Ok(new { success = true, message = "Xác thực cấu hình thành công." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await EnsureConfigAccessAsync(id);
            var result = await _service.DeleteConfigAsync(id);
            if (!result) return NotFound(new { success = false, message = "Không tìm thấy cấu hình." });
            await _auditLogService.LogAsync(
                HttpContext,
                "payment_config.delete",
                "PaymentConfig",
                id.ToString(),
                success: true);
            return Ok(new { success = true, message = "Đã xóa cấu hình thành công." });
        }

        private async Task EnsureConfigAccessAsync(Guid id)
        {
            var brandId = await _context.PaymentConfigs
                .AsNoTracking()
                .Where(config => config.Id == id)
                .Select(config => (Guid?)config.BrandId)
                .FirstOrDefaultAsync();

            if (brandId.HasValue)
            {
                await User.EnsureBrandAccessAsync(brandId.Value);
            }
        }

        private static string? MaskAccountNumber(string? accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                return null;
            }

            var trimmed = accountNumber.Trim();
            return trimmed.Length <= 4 ? "****" : $"****{trimmed[^4..]}";
        }
    }
}
