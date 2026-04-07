using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.DTOs.Brand;
using QLS.Backend.Models;
using QLS.Backend.Interfaces.Brand;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace QLS.Backend.Services.Brand
{
    // 2. Thực thi logic
    public class BrandService : IBrandService
    {
        private readonly AppDbContext _context;

        public BrandService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<BrandResponseDto>> GetAllBrandsAsync()
        {
            // Lấy danh sách từ DB và map sang DTO
            return await _context.Brands
                .Select(o => new BrandResponseDto
                {
                    Id = o.Id,
                    Name = o.Name,
                    Email = o.Email,
                    ContactPhone = o.ContactPhone,
                    IsActive = o.IsActive,
                    CreatedAt = o.CreatedAt
                })
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<BrandResponseDto> CreateBrandAsync(CreateBrandDto dto)
        {
            // Tạo entity mới từ DTO
            var newBrand = new Models.Brand
            {
                Name = dto.Name,
                Email = dto.Email,
                ContactPhone = dto.ContactPhone,
                IsActive = true, // Mặc định tạo ra là hoạt động luôn
                CreatedAt = DateTime.UtcNow
            };

            _context.Brands.Add(newBrand);
            await _context.SaveChangesAsync();

            // Trả về DTO sau khi tạo thành công
            return new BrandResponseDto
            {
                Id = newBrand.Id,
                Name = newBrand.Name,
                Email = newBrand.Email,
                ContactPhone = newBrand.ContactPhone,
                IsActive = newBrand.IsActive,
                CreatedAt = newBrand.CreatedAt
            };
        }

        public async Task<List<BrandAdminDto>> GetAllBrandAdminsAsync()
        {
            // Sử dụng Left Join để đảm bảo lấy được tài khoản ngay cả khi Profile (User) hoặc Brand bị thiếu
            var admins = await _context.Accounts
                .Where(acc => acc.Role == QLS.Backend.Models.Enums.UserRole.BrandAdmin)
                .Select(acc => new BrandAdminDto
                {
                    Id = acc.Id,
                    FullName = _context.Users.Where(u => u.Id == acc.Id).Select(u => u.FullName).FirstOrDefault() ?? "Chưa đặt tên",
                    Email = _context.Users.Where(u => u.Id == acc.Id).Select(u => u.Email).FirstOrDefault() ?? acc.Username,
                    BrandId = acc.BrandId,
                    BrandName = _context.Brands.Where(b => b.Id == acc.BrandId).Select(b => b.Name).FirstOrDefault() ?? "Không rõ chuỗi",
                    IsActive = acc.IsActive,
                    CreatedAt = acc.CreatedAt
                })
                .ToListAsync();

            return admins;
        }
    }
}
