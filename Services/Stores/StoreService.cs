using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.DTOs.Store;
using QLS.Backend.Interfaces.Stores;
using QLS.Backend.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QLS.Backend.Services.Stores
{
    public class StoreService : IStoreService
    {
        private readonly AppDbContext _context;

        public StoreService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Store>> GetStoresAsync()
        {
            return await _context.Stores.ToListAsync();
        }

        public async Task<int> GetStoreCountAsync()
        {
            return await _context.Stores.CountAsync();
        }

        public async Task<StoreResponseDto> CreateStoreAsync(CreateStoreDto dto)
        {
            if (dto.BrandId == Guid.Empty)
            {
                throw new ArgumentException("BrandId không hợp lệ.");
            }

            // Kiểm tra Brand có tồn tại không
            var brandExists = await _context.Brands.AnyAsync(b => b.Id == dto.BrandId);
            if (!brandExists)
            {
                throw new KeyNotFoundException("Không tìm thấy chuỗi sở hữu (Brand).");
            }

            // Kiểm tra xem Store có cùng tên trong cùng Brand đã tồn tại hay chưa
            var storeExists = await _context.Stores
                .AnyAsync(s => s.BrandId == dto.BrandId && s.Name.ToLower() == dto.Name.ToLower());
            if (storeExists)
            {
                throw new ArgumentException("Tên cửa hàng đã tồn tại trong chuỗi này.");
            }

            var newStore = new Store
            {
                Name = dto.Name,
                Address = dto.Address,
                BrandId = dto.BrandId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Stores.Add(newStore);
            await _context.SaveChangesAsync();

            // Ánh xạ sang response Dto
            return new StoreResponseDto
            {
                Id = newStore.Id,
                Name = newStore.Name,
                Address = newStore.Address,
                BrandId = newStore.BrandId,
                IsActive = newStore.IsActive,
                CreatedAt = newStore.CreatedAt
            };
        }
    }
}
