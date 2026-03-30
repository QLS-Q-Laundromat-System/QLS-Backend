using QLS.Backend.Services;
using QLS.Backend.Extensions;

namespace QLS.Backend.Extensions;

public static class ServiceExtensions
{
    public static void AddApplicationServices(this IServiceCollection services)
{
    // TỰ ĐỘNG QUÉT TẤT CẢ SERVICES
    services.Scan(scan => scan
        .FromAssembliesOf(typeof(IMachineDetailService))
        .AddClasses(classes => classes.InNamespaces("QLS.Backend.Services"))
        .AsImplementedInterfaces()
        .WithScopedLifetime());
}
}