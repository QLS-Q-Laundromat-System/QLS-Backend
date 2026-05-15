using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.DTOs.Brand;
using QLS.Backend.Interfaces.Brand;
using QLS.Backend.Models;

namespace QLS.Backend.Services.Brand
{
    public class PaymentConfigService : IPaymentConfigService
    {
        private readonly AppDbContext _context;

        public PaymentConfigService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PaymentConfigResponseDto> CreateConfigAsync(CreatePaymentConfigDto dto)
        {
            var brandExists = await _context.Brands.AnyAsync(b => b.Id == dto.BrandId);
            if (!brandExists) throw new Exception("Thương hiệu không tồn tại.");

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

            config.Provider = dto.Provider ?? config.Provider;
            config.BankCode = dto.BankCode ?? config.BankCode;
            config.AccountNumber = dto.AccountNumber ?? config.AccountNumber;
            config.AccountName = dto.AccountName ?? config.AccountName;
            config.ApiKey = dto.ApiKey ?? config.ApiKey;
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
