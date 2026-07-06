using Microsoft.Extensions.DependencyInjection;
using NhatTinLogistics.Sdk.Http;

namespace NhatTinLogistics.Sdk.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNhatTinLogisticsClient(
        this IServiceCollection services, Action<NhatTinLogisticsClientOptions> configure)
    {
        var options = new NhatTinLogisticsClientOptions();
        configure(options);
        options.Validate();

        services.AddSingleton(options);
        services.AddSingleton<ITokenStore, InMemoryTokenStore>();
        services.AddHttpClient<NhatTinHttpClient>((_, http) =>
        {
            http.BaseAddress = new Uri(options.ResolveBaseUrl());
            http.Timeout = TimeSpan.FromMilliseconds(options.TimeoutMilliseconds);
        });
        services.AddScoped(sp =>
            new NhatTinLogisticsClient(sp.GetRequiredService<NhatTinHttpClient>(), options));

        return services;
    }
}
