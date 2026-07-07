using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Application.Auth;
using NhatTinSandbox.Application.Bills;
using NhatTinSandbox.Application.Locations;
using NhatTinSandbox.Application.Webhooks;
using NhatTinSandbox.Infrastructure.Auth;
using NhatTinSandbox.Infrastructure.Bills;
using NhatTinSandbox.Infrastructure.Locations;
using NhatTinSandbox.Infrastructure.Persistence;
using NhatTinSandbox.Infrastructure.Webhooks;

namespace NhatTinSandbox.Api.Extensions;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddSandboxInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));

        var cs = config.GetConnectionString("Sandbox")
                 ?? "Server=192.168.200.8;Database=NhatTinSandbox;User Id=vipos;Password=CHANGE_ME;TrustServerCertificate=True;Encrypt=False";
        services.AddDbContext<SandboxDbContext>(o => o.UseSqlServer(cs));

        services.AddScoped<IAuthTokenService, JwtAuthTokenService>();
        services.AddScoped<ILocationCatalog, LocationCatalog>();
        services.AddScoped<IBillService, BillService>();
        services.AddScoped<IWebhookDispatcher, HttpWebhookDispatcher>();
        services.AddHttpClient("webhook");
        return services;
    }
}
