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

        public async Task<StoreResponseDto> GetStoreByIdAsync(Guid id)
        {
            var s = await _context.Stores.FindAsync(id);
            if (s == null)
            {
                throw new KeyNotFoundException("Không tìm thấy cửa hàng.");
            }
            return new StoreResponseDto
            {
                Id = s.Id,
                Name = s.Name,
                Address = s.Address,
                Phone = s.Phone,
                Email = s.Email,
                StoreId = s.StoreId,
                BrandId = s.BrandId,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            };
        }

        public async Task<StoreResponseDto> UpdateStoreAsync(Guid id, UpdateStoreDto dto)
        {
            var store = await _context.Stores.FindAsync(id);
            if (store == null)
            {
                throw new KeyNotFoundException("Không tìm thấy cửa hàng.");
            }

            // Check duplicate name within the same brand but not the same store
            var nameExists = await _context.Stores.AnyAsync(s => s.BrandId == store.BrandId && s.Id != id && s.Name.ToLower() == dto.Name.ToLower());
            if (nameExists)
            {
                throw new ArgumentException("Tên cửa hàng đã tồn tại trong chuỗi này.");
            }

            store.Name = dto.Name;
            store.Address = dto.Address;
            store.Phone = dto.Phone;
            store.Email = dto.Email;
            store.StoreId = dto.StoreId;
            store.IsActive = dto.IsActive;

            _context.Stores.Update(store);
            await _context.SaveChangesAsync();

            return new StoreResponseDto
            {
                Id = store.Id,
                Name = store.Name,
                Address = store.Address,
                Phone = store.Phone,
                Email = store.Email,
                StoreId = store.StoreId,
                BrandId = store.BrandId,
                IsActive = store.IsActive,
                CreatedAt = store.CreatedAt
            };
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
                Phone = dto.Phone,
                Email = dto.Email,
                StoreId = dto.StoreId,
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
                Phone = newStore.Phone,
                Email = newStore.Email,
                StoreId = newStore.StoreId,
                BrandId = newStore.BrandId,
                IsActive = newStore.IsActive,
                CreatedAt = newStore.CreatedAt
            };
        }

        public async Task<List<StoreAccountDto>> GetAccountsByStoreIdAsync(Guid storeId)
        {
            var accounts = await _context.Accounts
                .Where(acc => acc.StoreId == storeId && 
                             (acc.Role == QLS.Backend.Models.Enums.UserRole.Manager || 
                              acc.Role == QLS.Backend.Models.Enums.UserRole.Staff))
                .Select(acc => new StoreAccountDto
                {
                    Id = acc.Id,
                    Username = acc.Username,
                    FullName = _context.Users.Where(u => u.Id == acc.Id).Select(u => u.FullName).FirstOrDefault() ?? acc.Username,
                    Email = _context.Users.Where(u => u.Id == acc.Id).Select(u => u.Email).FirstOrDefault() ?? "",
                    Role = acc.Role.ToString(),
                    BrandId = acc.BrandId,
                    IsActive = acc.IsActive,
                    CreatedAt = acc.CreatedAt
                })
                .ToListAsync();

            return accounts;
        }

        public async Task<List<Machine>> GetMachinesByStoreIdAsync(Guid storeId)
        {
            return await _context.Machines
                .Where(m => m.StoreId == storeId)
                .ToListAsync();
        }
    }
}
