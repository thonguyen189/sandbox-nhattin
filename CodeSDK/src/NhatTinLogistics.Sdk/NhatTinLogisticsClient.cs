using System.Net.Http;
using NhatTinLogistics.Sdk.Client;
using NhatTinLogistics.Sdk.Http;

namespace NhatTinLogistics.Sdk;

/// <summary>Entry point. Exposes Auth / Bill / Location. Use standalone or resolve from DI.</summary>
public sealed class NhatTinLogisticsClient : IDisposable
{
    // Set ONLY by the standalone ctor, which news up its own HttpClient and therefore owns it.
    // Null on the DI path, where the HttpClient is owned by the container / NhatTinHttpClient.
    private readonly HttpClient? _ownedHttp;

    public IAuthApi Auth { get; }
    public IBillApi Bill { get; }
    public ILocationApi Location { get; }

    /// <summary>Token store backing this client. Seed a token here when AutoAuthenticate is false.</summary>
    public ITokenStore Tokens { get; }

    /// <summary>DI-friendly constructor. The typed NhatTinHttpClient is supplied by the container.</summary>
    public NhatTinLogisticsClient(NhatTinHttpClient http, NhatTinLogisticsClientOptions options, ITokenStore tokens)
    {
        Tokens = tokens;
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
        _ownedHttp = httpClient;

        var store = new InMemoryTokenStore();
        Tokens = store;
        var nt = new NhatTinHttpClient(httpClient, options, store);
        Auth = new AuthApi(nt);
        Bill = new BillApi(nt, options);
        Location = new LocationApi(nt);
    }

    /// <summary>Disposes the HttpClient owned by the standalone ctor. No-op on the DI path.</summary>
    public void Dispose() => _ownedHttp?.Dispose();
}
