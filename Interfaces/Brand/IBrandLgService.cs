using QLS.Backend.DTOs.Lg;
using System.Threading.Tasks;

namespace QLS.Backend.Interfaces.Brand
{
    public interface IBrandLgService
    {
        /// <summary>
        /// Thực hiện đăng nhập LG và lưu thông tin Access Token vào Brand.
        /// </summary>
        Task<LgAuthTokenResult> LinkLgAccountAsync(Guid brandId, LgLoginRequest loginRequest);

        /// <summary>
        /// Làm mới token cho Brand sử dụng Refresh Token đã lưu trong DB.
        /// </summary>
        Task<LgAuthTokenResult> RefreshBrandTokenAsync(Guid brandId);

        /// <summary>
        /// Đồng bộ danh sách cửa hàng từ LG ThinQ về database địa phương.
        /// </summary>
        Task<int> SyncBrandStoresAsync(Guid brandId);
    }
}
