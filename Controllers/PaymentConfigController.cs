using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs.Brand;
using QLS.Backend.Interfaces.Brand;
using System;
using System.Threading.Tasks;

namespace QLS.Backend.Controllers
{
    [ApiController]
    [Route("api/v1/payment-configs")]
    public class PaymentConfigController : ControllerBase
    {
        private readonly IPaymentConfigService _service;

        public PaymentConfigController(IPaymentConfigService service)
        {
            _service = service;
        }

        [HttpPost("brand/{brandId}")]
        public async Task<IActionResult> Create(Guid brandId, [FromBody] CreatePaymentConfigDto dto)
        {
            try
            {
                dto.BrandId = brandId;
                var result = await _service.CreateConfigAsync(dto);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("brand/{brandId}")]
        public async Task<IActionResult> GetByBrand(Guid brandId)
        {
            var result = await _service.GetConfigsByBrandAsync(brandId);
            return Ok(new { success = true, data = result });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
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
                var result = await _service.UpdateConfigAsync(id, dto);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPatch("{id}/activate")]
        public async Task<IActionResult> Activate(Guid id)
        {
            var result = await _service.ActivateConfigAsync(id);
            if (!result) return NotFound(new { success = false, message = "Không tìm thấy cấu hình." });
            return Ok(new { success = true, message = "Đã kích hoạt cấu hình thành công." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteConfigAsync(id);
            if (!result) return NotFound(new { success = false, message = "Không tìm thấy cấu hình." });
            return Ok(new { success = true, message = "Đã xóa cấu hình thành công." });
        }
    }
}
