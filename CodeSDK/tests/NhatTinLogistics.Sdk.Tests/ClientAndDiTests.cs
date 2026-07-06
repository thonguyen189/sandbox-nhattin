using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using NhatTinLogistics.Sdk;
using NhatTinLogistics.Sdk.Extensions;
using NhatTinLogistics.Sdk.Tests.Infrastructure;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class ClientAndDiTests
{
    // StubHttpMessageHandler is sealed, so this small handler flags disposal for the IDisposable test.
    private sealed class DisposeTrackingHandler : HttpMessageHandler
    {
        public bool Disposed { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"success\":true,\"data\":{}}", Encoding.UTF8, "application/json"),
            });

        protected override void Dispose(bool disposing)
        {
            if (disposing) Disposed = true;
            base.Dispose(disposing);
        }
    }

    [Fact]
    public void Standalone_client_dispose_disposes_owned_handler()
    {
        var handler = new DisposeTrackingHandler();
        var client = new NhatTinLogisticsClient(
            new NhatTinLogisticsClientOptions { Username = "u", Password = "p", BaseUrl = "https://test.local" },
            handler);

        Assert.False(handler.Disposed);
        client.Dispose();
        // Disposing the client disposes its owned HttpClient, which disposes the handler.
        Assert.True(handler.Disposed);
    }

    [Fact]
    public async Task Manual_token_mode_seeds_token_and_does_not_call_auth_on_401()
    {
        var handler = new StubHttpMessageHandler(_ =>
            TestResponses.Json(HttpStatusCode.Unauthorized, "{\"success\":false,\"message\":\"expired\"}"));

        var client = new NhatTinLogisticsClient(
            new NhatTinLogisticsClientOptions { BaseUrl = "https://test.local", AutoAuthenticate = false },
            handler);
        client.Tokens.SetTokens("SEEDED", "R");

        var resp = await client.Bill.CreateAsync(new Types.Requests.CreateBillRequest { Weight = 1 });

        // The mapped 401 surfaces as a failure; the SDK never signs in or refreshes.
        Assert.False(resp.IsSuccess);
        Assert.Equal(401, resp.HttpStatusCode);
        Assert.DoesNotContain(handler.Requests, r => r.RequestUri!.AbsolutePath.EndsWith("/sign-in"));
        Assert.DoesNotContain(handler.Requests, r => r.RequestUri!.AbsolutePath.EndsWith("/refresh-token"));
        Assert.Single(handler.Requests); // only the single business call happened
    }

    [Fact]
    public async Task Manual_token_mode_attaches_seeded_bearer_token()
    {
        var handler = new StubHttpMessageHandler(_ =>
            TestResponses.Ok("{\"success\":true,\"data\":{\"bill_code\":\"CP1\"}}"));

        var client = new NhatTinLogisticsClient(
            new NhatTinLogisticsClientOptions { BaseUrl = "https://test.local", AutoAuthenticate = false },
            handler);
        client.Tokens.SetTokens("SEEDED", "R");

        var resp = await client.Bill.CreateAsync(new Types.Requests.CreateBillRequest { Weight = 1 });

        Assert.True(resp.IsSuccess);
        Assert.Single(handler.Requests);
        Assert.Equal("SEEDED", handler.Requests[0].Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task Standalone_client_creates_bill_end_to_end_via_handler()
    {
        var handler = new StubHttpMessageHandler(req =>
            req.RequestUri!.AbsolutePath.EndsWith("/sign-in")
                ? TestResponses.Ok("{\"success\":true,\"data\":{\"jwt_token\":\"A\",\"refresh_token\":\"R\"}}")
                : TestResponses.Ok("{\"success\":true,\"data\":{\"bill_code\":\"CP1\"}}"));

        var client = new NhatTinLogisticsClient(
            new NhatTinLogisticsClientOptions { Username = "u", Password = "p", BaseUrl = "https://test.local" },
            handler);

        var resp = await client.Bill.CreateAsync(new Types.Requests.CreateBillRequest { Weight = 1 });

        Assert.True(resp.IsSuccess);
        Assert.Equal("CP1", resp.Data!.BillCode);
    }

    [Fact]
    public void Di_registration_resolves_client()
    {
        var services = new ServiceCollection();
        services.AddNhatTinLogisticsClient(o =>
        {
            o.Username = "u"; o.Password = "p"; o.BaseUrl = "https://test.local";
        });
        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<NhatTinLogisticsClient>();
        Assert.NotNull(client.Auth);
        Assert.NotNull(client.Bill);
        Assert.NotNull(client.Location);
    }

    [Fact]
    public void Di_registration_validates_options()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentException>(() =>
            services.AddNhatTinLogisticsClient(o => { /* no username/password, AutoAuthenticate default true */ }));
    }
}
