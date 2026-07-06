using System.Net.Http;
using NhatTinLogistics.Sdk.Client;
using NhatTinLogistics.Sdk.Http;

namespace NhatTinLogistics.Sdk;

/// <summary>Entry point. Exposes Auth / Bill / Location. Use standalone or resolve from DI.</summary>
public sealed class NhatTinLogisticsClient
{
    public IAuthApi Auth { get; }
    public IBillApi Bill { get; }
    public ILocationApi Location { get; }

    /// <summary>DI-friendly constructor. The typed NhatTinHttpClient is supplied by the container.</summary>
    public NhatTinLogisticsClient(NhatTinHttpClient http, NhatTinLogisticsClientOptions options)
    {
        Auth = new AuthApi(http);
        Bill = new BillApi(http, options);
        Location = new LocationApi(http);
    }

    /// <summary>Standalone constructor. Owns its HttpClient. Pass a handler for tests.</summary>
    public NhatTinLogisticsClient(NhatTinLogisticsClientOptions options, HttpMessageHandler? handler = null)
    {
        options.Validate();
        var httpClient = handler is null ? new HttpClient() : new HttpClient(handler);
        httpClient.BaseAddress = new Uri(options.ResolveBaseUrl());
        httpClient.Timeout = TimeSpan.FromMilliseconds(options.TimeoutMilliseconds);

        var nt = new NhatTinHttpClient(httpClient, options, new InMemoryTokenStore());
        Auth = new AuthApi(nt);
        Bill = new BillApi(nt, options);
        Location = new LocationApi(nt);
    }
}
