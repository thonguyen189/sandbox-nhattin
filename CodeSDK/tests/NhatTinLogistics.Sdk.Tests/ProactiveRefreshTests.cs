using System;
using System.Linq;
using System.Net.Http;
using NhatTinLogistics.Sdk;
using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Tests.Infrastructure;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class ProactiveRefreshTests
{
    private const string SignInBody =
        "{\"success\":true,\"data\":{\"jwt_token\":\"ACCESS\",\"refresh_token\":\"REFRESH\",\"token_type\":\"Bearer\"," +
        "\"token_expires_in\":\"24h\",\"refresh_expires_in\":\"7d\"}}";
    private const string RefreshBody =
        "{\"success\":true,\"data\":{\"jwt_token\":\"ACCESS2\",\"refresh_token\":\"REFRESH2\"," +
        "\"token_expires_in\":\"24h\",\"refresh_expires_in\":\"7d\"}}";

    private static (NhatTinHttpClient client, StubHttpMessageHandler handler, InMemoryTokenStore store) Build(
        Func<DateTimeOffset> clock,
        Func<HttpRequestMessage, HttpResponseMessage> responder,
        Action<NhatTinLogisticsClientOptions>? configure = null)
    {
        var options = new NhatTinLogisticsClientOptions { Username = "u", Password = "p", BaseUrl = "https://test.local" };
        configure?.Invoke(options);
        var handler = new StubHttpMessageHandler(responder);
        var http = new HttpClient(handler) { BaseAddress = new Uri(options.ResolveBaseUrl()) };
        var store = new InMemoryTokenStore();
        return (new NhatTinHttpClient(http, options, store, clock), handler, store);
    }

    private static Func<HttpRequestMessage, HttpResponseMessage> DefaultResponder =>
        req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.EndsWith("/sign-in")) return TestResponses.Ok(SignInBody);
            if (path.EndsWith("/refresh-token")) return TestResponses.Ok(RefreshBody);
            return TestResponses.Ok("{\"success\":true,\"data\":1}");
        };

    private static int Count(StubHttpMessageHandler h, string suffix)
        => h.Requests.Count(r => r.RequestUri!.AbsolutePath.EndsWith(suffix));

    [Fact]
    public async Task Access_token_near_expiry_is_refreshed_before_next_call_without_401()
    {
        var now = new DateTimeOffset(2026, 7, 7, 0, 0, 0, TimeSpan.Zero);
        var (client, handler, store) = Build(() => now, DefaultResponder);

        // First call signs in and stores ACCESS with expiry = now + 24h.
        await client.GetAsync<int>("/v3/bill/tracking?bill_code=A", default);
        Assert.Equal("ACCESS", store.AccessToken);

        // Advance to 30s before expiry — inside the 60s skew window.
        now = now.AddHours(24).AddSeconds(-30);

        var resp = await client.GetAsync<int>("/v3/bill/tracking?bill_code=B", default);

        Assert.True(resp.IsSuccess);
        Assert.Equal("ACCESS2", store.AccessToken);
        Assert.Equal(1, Count(handler, "/refresh-token"));  // proactively refreshed, exactly once
        var lastBusiness = handler.Requests.Last(r => r.RequestUri!.AbsolutePath.EndsWith("/tracking"));
        Assert.Equal("ACCESS2", lastBusiness.Headers.Authorization!.Parameter); // second call used the new token
    }

    [Fact]
    public async Task No_proactive_refresh_when_ttl_is_unparseable()
    {
        var now = new DateTimeOffset(2026, 7, 7, 0, 0, 0, TimeSpan.Zero);
        // sign-in omits token_expires_in → no expiry stored → proactive disabled for this token.
        const string signInNoTtl = "{\"success\":true,\"data\":{\"jwt_token\":\"ACCESS\",\"refresh_token\":\"REFRESH\"}}";
        var (client, handler, store) = Build(() => now, req =>
            req.RequestUri!.AbsolutePath.EndsWith("/sign-in")
                ? TestResponses.Ok(signInNoTtl)
                : TestResponses.Ok("{\"success\":true,\"data\":1}"));

        await client.GetAsync<int>("/v3/bill/tracking?bill_code=A", default);
        now = now.AddDays(30); // way past any real expiry

        await client.GetAsync<int>("/v3/bill/tracking?bill_code=B", default);

        Assert.Equal(0, Count(handler, "/refresh-token")); // never refreshed proactively
        Assert.Equal(1, Count(handler, "/sign-in"));       // signed in once, token reused
        Assert.Equal("ACCESS", store.AccessToken);
    }

    [Fact]
    public async Task Proactive_refresh_can_be_disabled_by_option()
    {
        var now = new DateTimeOffset(2026, 7, 7, 0, 0, 0, TimeSpan.Zero);
        var (client, handler, _) = Build(() => now, DefaultResponder, o => o.EnableProactiveRefresh = false);

        await client.GetAsync<int>("/v3/bill/tracking?bill_code=A", default);
        now = now.AddHours(24).AddSeconds(-30); // inside skew, but proactive is off

        await client.GetAsync<int>("/v3/bill/tracking?bill_code=B", default);

        Assert.Equal(0, Count(handler, "/refresh-token")); // disabled → no proactive refresh
    }

    [Fact]
    public async Task Expired_refresh_token_forces_full_sign_in_not_refresh()
    {
        var now = new DateTimeOffset(2026, 7, 7, 0, 0, 0, TimeSpan.Zero);
        var (client, handler, store) = Build(() => now, DefaultResponder);

        await client.GetAsync<int>("/v3/bill/tracking?bill_code=A", default);
        now = now.AddDays(8); // both access (24h) and refresh (7d) expired

        await client.GetAsync<int>("/v3/bill/tracking?bill_code=B", default);

        Assert.Equal(0, Count(handler, "/refresh-token")); // refresh token expired → not used
        Assert.Equal(2, Count(handler, "/sign-in"));       // signed in again instead
        Assert.Equal("ACCESS", store.AccessToken);
    }
}
