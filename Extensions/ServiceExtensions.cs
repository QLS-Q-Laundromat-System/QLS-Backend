using QLS.Backend.Services;
using QLS.Backend.Extensions;

namespace QLS.Backend.Extensions;

public static class ServiceExtensions
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        // 1. TỰ ĐỘNG QUÉT VÀ ĐĂNG KÝ CÁC SERVICE THÔNG THƯỜNG
        services.Scan(scan => scan
            .FromAssembliesOf(typeof(IMachineDetailService)) // Bắt đầu quét từ Assembly chứa ILgService
            .AddClasses(classes => classes.InNamespaces("QLS.Backend.Services")) // Chỉ quét trong thư mục Services
            .AsImplementedInterfaces() // Tự động khớp IMachineService với MachineService
            .WithScopedLifetime());    // Đăng ký theo kiểu Scoped (mặc định cho web)

        // 2. NGOẠI LỆ: NHỮNG SERVICE DÙNG HTTPCLIENT (Như LG API)
        // Vì AddHttpClient làm nhiều việc hơn là chỉ đăng ký (nó quản lý cả kết nối mạng),
        // nên những cái nào dùng API ngoài Sơn vẫn nên để riêng 1 dòng cho rõ ràng.
        services.AddHttpClient<MachineDetailService, MachineDetailService>();
    }
}