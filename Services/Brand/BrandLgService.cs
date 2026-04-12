using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.DTOs.Lg;
using QLS.Backend.Exceptions;
using QLS.Backend.Interfaces.Brand;
using QLS.Backend.Interfaces.LG;
using QLS.Backend.Models;
using QLS.Backend.Services.LgService;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace QLS.Backend.Services.Brand
{
    public class BrandLgService : IBrandLgService
    {
        private readonly AppDbContext _context;
        private readonly ILgAuthTokenService _lgAuthService;
        private readonly LgApiClient _lgApiClient;
        private readonly ILogger<BrandLgService> _logger;

        public BrandLgService(
            AppDbContext context,
            ILgAuthTokenService lgAuthService,
            LgApiClient lgApiClient,
            ILogger<BrandLgService> logger)
        {
            _context = context;
            _lgAuthService = lgAuthService;
            _lgApiClient = lgApiClient;
            _logger = logger;
        }

        public async Task<LgAuthTokenResult> LinkLgAccountAsync(Guid brandId, LgLoginRequest loginRequest)
        {
            // 1. Kiểm tra Brand tồn tại
            var brand = await _context.Brands.FindAsync(brandId);
            if (brand == null) throw new ApiException("Không tìm thấy Brand để liên kết.", 404);

            // 2. Gọi flow lấy token từ LG
            _logger.LogInformation("[BrandLg] Đang lấy token LG cho Brand: {BrandName}", brand.Name);
            var result = await _lgAuthService.GetAccessTokenAsync(loginRequest);

            // 3. Tìm hoặc tạo mới Credential
            var credential = await _context.BrandLgCredentials.FindAsync(brandId);
            if (credential == null)
            {
                credential = new BrandLgCredential { BrandId = brandId };
                _context.BrandLgCredentials.Add(credential);
            }

            // 4. Cập nhật thông tin
            credential.LgEmail       = loginRequest.Email;
            credential.UserAuthHash  = _lgAuthService.HashPassword(loginRequest.Password);
            credential.LgUserNo      = result.UserNo;
            credential.AccessToken   = result.AccessToken;
            credential.RefreshToken  = result.RefreshToken;
            credential.Oauth2BackendUrl = result.Oauth2BackendUrl;
            credential.TokenExpiresAt = DateTime.UtcNow.AddSeconds(result.ExpiresIn);
            credential.UpdatedAt     = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("[BrandLg] ✅ Đã lưu token LG cho Brand: {BrandName}", brand.Name);

            return result;
        }

        public async Task<LgAuthTokenResult> RefreshBrandTokenAsync(Guid brandId)
        {
            // 1. Lấy Credential từ DB
            var credential = await _context.BrandLgCredentials.FindAsync(brandId);
            if (credential == null || string.IsNullOrEmpty(credential.RefreshToken))
                throw new ApiException("Brand này chưa được liên kết tài khoản LG hoặc thiếu Refresh Token.", 400);

            // 2. Gọi LG API để refresh (ưu tiên dùng Oauth2BackendUrl đã lưu)
            _logger.LogInformation("[BrandLg] Đang refresh token cho BrandId: {Id}", brandId);
            var backendUrl = credential.Oauth2BackendUrl ?? "https://kr.biz.lgeapi.com/";
            var result = await _lgAuthService.RefreshAccessTokenAsync(credential.RefreshToken, backendUrl);

            // 3. Cập nhật lại vào DB
            credential.AccessToken    = result.AccessToken;
            if (!string.IsNullOrEmpty(result.RefreshToken))
                credential.RefreshToken = result.RefreshToken;
            
            credential.Oauth2BackendUrl = result.Oauth2BackendUrl;
            credential.TokenExpiresAt = DateTime.UtcNow.AddSeconds(result.ExpiresIn);
            credential.UpdatedAt      = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("[BrandLg] ✅ Đã cập nhật token mới cho BrandId: {Id}", brandId);

            return result;
        }

        public async Task<int> SyncBrandStoresAsync(Guid brandId)
        {
            // 1. Lấy thông tin xác thực từ DB
            var credential = await _context.BrandLgCredentials.FindAsync(brandId);
            if (credential == null || string.IsNullOrEmpty(credential.AccessToken) || string.IsNullOrEmpty(credential.LgUserNo))
                throw new ApiException("Brand này chưa được liên kết hoặc chưa có token.", 400);

            // 2. Gọi API lấy danh sách Store từ ThinQ
            _logger.LogInformation("[BrandLg] Đang gọi ThinQ API để lấy danh sách cửa hàng cho BrandId: {Id}", brandId);
            var json = await _lgApiClient.GetStoresAsync(credential.LgUserNo, credential.AccessToken);
            
            var response = JsonSerializer.Deserialize<LgThinqStoreListResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (response == null || response.ResultCode != "0000")
                throw new ApiException($"Lỗi từ LG ThinQ: {response?.ResultCode ?? "Unknown Error"}", 500);

            var lgStores = response.Result.Stores;
            _logger.LogInformation("[BrandLg] Tìm thấy {Count} cửa hàng từ LG.", lgStores.Count);

            int syncCount = 0;
            foreach (var lgStore in lgStores)
            {
                // Kiểm tra xem store đã tồn tại chưa (dựa trên LG storeId)
                var existingStore = await _context.Stores.FirstOrDefaultAsync(s => s.StoreId == lgStore.StoreId);
                if (existingStore == null)
                {
                    // Tạo mới
                    var newStore = new Store
                    {
                        Id = Guid.NewGuid(),
                        StoreId = lgStore.StoreId,
                        Name = lgStore.StoreName,
                        Email = lgStore.Email,
                        Phone = lgStore.Phone,
                        Address = lgStore.Address.FullAddress,
                        BrandId = brandId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Stores.Add(newStore);
                    syncCount++;
                }
                else
                {
                    // Cập nhật thông tin nếu cần
                    existingStore.Name = lgStore.StoreName;
                    existingStore.Email = lgStore.Email;
                    existingStore.Phone = lgStore.Phone;
                    existingStore.Address = lgStore.Address.FullAddress;
                    existingStore.BrandId = brandId; // Đảm bảo đúng Brand
                }
            }

            await _context.SaveChangesAsync();
            return syncCount;
        }
    }
}
