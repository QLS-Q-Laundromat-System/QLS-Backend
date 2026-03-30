using QLS.Backend.Services;
using QLS.Backend.Extensions;

namespace QLS.Backend.Extensions;

public static class ServiceExtensions
{
    public static void AddApplicationServices(this IServiceCollection services)
{
    // 1. TỰ ĐỘNG QUÉT (Nên loại trừ MachineDetailService để tránh đăng ký 2 lần)
    services.Scan(scan => scan
        .FromAssembliesOf(typeof(IMachineDetailService))
        .AddClasses(classes => classes.InNamespaces("QLS.Backend.Services")
                                      .Where(c => c.Name != "MachineDetailService")) // Loại trừ ra
        .AsImplementedInterfaces()
        .WithScopedLifetime());

    // 2. ĐĂNG KÝ RIÊNG CHO HTTPCLIENT (Bắt buộc dùng Interface)
    services.AddHttpClient<IMachineDetailService, MachineDetailService>();
}
}