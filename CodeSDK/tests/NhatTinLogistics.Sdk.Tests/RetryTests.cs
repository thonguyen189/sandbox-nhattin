using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NhatTinLogistics.Sdk;
using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Tests.Infrastructure;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class RetryTests
{
    private const string SignInBody =
        "{\"success\":true,\"data\":{\"jwt_token\":\"ACCESS\",\"refresh_token\":\"REFRESH\"}}";

    // No-op delay so retries run instantly — no real backoff sleeping in tests.
    private static readonly Func<TimeSpan, CancellationToken, Task> NoDelay = (_, __) => Task.CompletedTask;

    private static (NhatTinHttpClient client, StubHttpMessageHandler handler) Build(
        Func<HttpRequestMessage, HttpResponseMessage> responder,
        Action<NhatTinLogisticsClientOptions>? configure = null)
    {
        var options = new NhatTinLogisticsClientOptions { Username = "u", Password = "p", BaseUrl = "https://test.local" };
        configure?.Invoke(options);
        var handler = new StubHttpMessageHandler(responder);
        var http = new HttpClient(handler) { BaseAddress = new Uri(options.ResolveBaseUrl()) };
        var store = new InMemoryTokenStore();
        return (new NhatTinHttpClient(http, options, store, null, NoDelay), handler);
    }

    private static int Count(StubHttpMessageHandler h, string suffix)
        => h.Requests.Count(r => r.RequestUri!.AbsolutePath.EndsWith(suffix));

    [Fact]
    public async Task Get_retries_on_5xx_then_succeeds()
    {
        var phase = 0;
        var (client, handler) = Build(req =>
        {
            if (req.RequestUri!.AbsolutePath.EndsWith("/sign-in")) return TestResponses.Ok(SignInBody);
            return phase++ < 2
                ? TestResponses.Json(HttpStatusCode.InternalServerError, "{\"success\":false,\"message\":\"boom\"}")
                : TestResponses.Ok("{\"success\":true,\"data\":7}");
        });

        var resp = await client.GetAsync<int>("/v3/bill/tracking?bill_code=X", default);

        Assert.True(resp.IsSuccess);
        Assert.Equal(7, resp.Data);
        Assert.Equal(3, Count(handler, "/tracking")); // 2 failures + 1 success
    }

    [Fact]
    public async Task Get_retries_on_429_then_succeeds()
    {
        var phase = 0;
        var (client, handler) = Build(req =>
        {
            if (req.RequestUri!.AbsolutePath.EndsWith("/sign-in")) return TestResponses.Ok(SignInBody);
            return phase++ == 0
                ? TestResponses.Json(HttpStatusCode.TooManyRequests, "{\"success\":false}")
                : TestResponses.Ok("{\"success\":true,\"data\":1}");
        });

        var resp = await client.GetAsync<int>("/v3/bill/tracking?bill_code=X", default);

        Assert.True(resp.IsSuccess);
        Assert.Equal(2, Count(handler, "/tracking"));
    }

    [Fact]
    public async Task Get_retries_on_transport_failure()
    {
        var phase = 0;
        var (client, handler) = Build(req =>
        {
            if (req.RequestUri!.AbsolutePath.EndsWith("/sign-in")) return TestResponses.Ok(SignInBody);
            if (phase++ == 0) throw new HttpRequestException("connection reset");
            return TestResponses.Ok("{\"success\":true,\"data\":1}");
        });

        var resp = await client.GetAsync<int>("/v3/bill/tracking?bill_code=X", default);

        Assert.True(resp.IsSuccess);
        Assert.Equal(2, Count(handler, "/tracking"));
    }

    [Fact]
    public async Task Create_is_not_retried_on_5xx()
    {
        var (client, handler) = Build(req =>
            req.RequestUri!.AbsolutePath.EndsWith("/sign-in")
                ? TestResponses.Ok(SignInBody)
                : TestResponses.Json(HttpStatusCode.InternalServerError, "{\"success\":false,\"message\":\"server\"}"));

        var resp = await client.PostAsync<object>("/v3/bill/create", new { weight = 2 }, default);

        Assert.False(resp.IsSuccess);
        Assert.Equal(500, resp.HttpStatusCode);
        Assert.Equal(1, Count(handler, "/create")); // write is never retried — could double-create
    }

    [Fact]
    public async Task Business_error_200_is_not_retried()
    {
        var (client, handler) = Build(req =>
            req.RequestUri!.AbsolutePath.EndsWith("/sign-in")
                ? TestResponses.Ok(SignInBody)
                : TestResponses.Ok("{\"success\":false,\"message\":\"bad input\"}"));

        var resp = await client.GetAsync<int>("/v3/bill/tracking?bill_code=X", default);

        Assert.False(resp.IsSuccess);
        Assert.Equal(1, Count(handler, "/tracking")); // HTTP 200 business error is a real answer
    }

    [Fact]
    public async Task Get_exhausts_retries_then_surfaces_last_response()
    {
        var (client, handler) = Build(
            req => req.RequestUri!.AbsolutePath.EndsWith("/sign-in")
                ? TestResponses.Ok(SignInBody)
                : TestResponses.Json(HttpStatusCode.ServiceUnavailable, "{\"success\":false,\"message\":\"down\"}"),
            o => o.MaxRetries = 3);

        var resp = await client.GetAsync<int>("/v3/bill/tracking?bill_code=X", default);

        Assert.False(resp.IsSuccess);
        Assert.Equal(503, resp.HttpStatusCode);
        Assert.Equal(4, Count(handler, "/tracking")); // 1 initial + 3 retries
    }

    [Fact]
    public async Task Retry_disabled_by_option_surfaces_first_5xx()
    {
        var (client, handler) = Build(
            req => req.RequestUri!.AbsolutePath.EndsWith("/sign-in")
                ? TestResponses.Ok(SignInBody)
                : TestResponses.Json(HttpStatusCode.InternalServerError, "{\"success\":false}"),
            o => o.EnableRetry = false);

        var resp = await client.GetAsync<int>("/v3/bill/tracking?bill_code=X", default);

        Assert.False(resp.IsSuccess);
        Assert.Equal(1, Count(handler, "/tracking"));
    }
}
