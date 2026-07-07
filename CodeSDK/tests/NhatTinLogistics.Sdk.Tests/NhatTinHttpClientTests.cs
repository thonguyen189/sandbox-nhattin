using System.Net;
using System.Net.Http;
using NhatTinLogistics.Sdk;
using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Tests.Infrastructure;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class NhatTinHttpClientTests
{
    private static (NhatTinHttpClient client, StubHttpMessageHandler handler, InMemoryTokenStore store) Build(
        Func<HttpRequestMessage, HttpResponseMessage> responder,
        Action<NhatTinLogisticsClientOptions>? configure = null)
    {
        var options = new NhatTinLogisticsClientOptions { Username = "u", Password = "p", BaseUrl = "https://test.local" };
        configure?.Invoke(options);
        var handler = new StubHttpMessageHandler(responder);
        var http = new HttpClient(handler) { BaseAddress = new Uri(options.ResolveBaseUrl()) };
        var store = new InMemoryTokenStore();
        return (new NhatTinHttpClient(http, options, store), handler, store);
    }

    private const string SignInBody =
        "{\"success\":true,\"data\":{\"jwt_token\":\"ACCESS\",\"refresh_token\":\"REFRESH\",\"token_type\":\"Bearer\"}}";

    [Fact]
    public async Task Post_success_maps_envelope_and_signs_in_first()
    {
        var (client, handler, store) = Build(req =>
            req.RequestUri!.AbsolutePath.EndsWith("/sign-in")
                ? TestResponses.Ok(SignInBody)
                : TestResponses.Ok("{\"success\":true,\"message\":\"ok\",\"data\":{\"bill_code\":\"CP1\"}}"));

        var resp = await client.PostAsync<Dictionary<string, string>>("/v3/bill/create", new { weight = 2 }, default);

        Assert.True(resp.IsSuccess);
        Assert.Equal("CP1", resp.Data!["bill_code"]);
        Assert.Equal("ACCESS", store.AccessToken);
        // sign-in call then the real call
        Assert.Equal(2, handler.CallCount);
        Assert.Contains("Bearer ACCESS", handler.Requests[1].Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task SignIn_captures_partner_id_into_options()
    {
        NhatTinLogisticsClientOptions? captured = null;
        var (client, _, _) = Build(
            req => req.RequestUri!.AbsolutePath.EndsWith("/sign-in")
                ? TestResponses.Ok("{\"success\":true,\"data\":{\"jwt_token\":\"tok\",\"partner_id\":124823}}")
                : TestResponses.Ok("{\"success\":true,\"data\":1}"),
            configure: o =>
            {
                o.AutoAuthenticate = true;
                captured = o;
            });

        var resp = await client.GetAsync<int>("/v3/bill/tracking?bill_code=CP1", default);

        Assert.True(resp.IsSuccess);
        Assert.NotNull(captured);
        Assert.Equal(124823, captured!.PartnerId);
    }

    [Fact]
    public async Task Business_failure_does_not_throw()
    {
        var (client, _, _) = Build(req =>
            req.RequestUri!.AbsolutePath.EndsWith("/sign-in")
                ? TestResponses.Ok(SignInBody)
                : TestResponses.Ok("{\"success\":false,\"message\":\"bad input\",\"data\":null}"));

        var resp = await client.PostAsync<object>("/v3/bill/create", new { }, default);

        Assert.False(resp.IsSuccess);
        Assert.Equal("bad input", resp.Message);
    }

    [Fact]
    public async Task Unauthorized_triggers_single_refresh_then_retry()
    {
        var phase = 0;
        var (client, handler, store) = Build(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.EndsWith("/sign-in")) return TestResponses.Ok(SignInBody);
            if (path.EndsWith("/refresh-token"))
                return TestResponses.Ok("{\"success\":true,\"data\":{\"jwt_token\":\"ACCESS2\",\"refresh_token\":\"REFRESH2\"}}");
            // first business call 401, second OK
            return phase++ == 0
                ? TestResponses.Json(HttpStatusCode.Unauthorized, "{\"success\":false,\"message\":\"expired\"}")
                : TestResponses.Ok("{\"success\":true,\"data\":1}");
        });

        var resp = await client.GetAsync<int>("/v3/bill/tracking?bill_code=CP1", default);

        Assert.True(resp.IsSuccess);
        Assert.Equal(1, resp.Data);
        Assert.Equal("ACCESS2", store.AccessToken);
        Assert.Contains(handler.Requests, r => r.RequestUri!.AbsolutePath.EndsWith("/refresh-token"));
    }

    [Fact]
    public async Task Concurrent_401s_refresh_only_once()
    {
        // Ensure the pool can hand out 5 threads at once so all callers can sit in the barrier
        // together without starving the pool.
        ThreadPool.GetMinThreads(out var w, out var c);
        ThreadPool.SetMinThreads(Math.Max(w, 8), c);

        var refreshCount = 0;
        // Barrier: hold every stale-token (ACCESS) 401 until all 5 callers have arrived, so they
        // all reach RefreshIfStaleAsync's single-flight guard simultaneously. If the guard were
        // removed this would yield refreshCount == 5 (or hang → timeout → fail) instead of 1.
        using var gate = new System.Threading.CountdownEvent(5);

        var (client, _, store) = Build(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.EndsWith("/sign-in")) return TestResponses.Ok(SignInBody);
            if (path.EndsWith("/refresh-token"))
            {
                Interlocked.Increment(ref refreshCount);
                return TestResponses.Ok("{\"success\":true,\"data\":{\"jwt_token\":\"ACCESS2\",\"refresh_token\":\"REFRESH2\"}}");
            }
            // A business call 401s only while the caller still holds the stale token. Block here
            // until all 5 stale callers have arrived, forcing concurrent contention on the refresh
            // guard. The refresh (/refresh-token) and the ACCESS2 retries below never touch the gate.
            if (req.Headers.Authorization?.Parameter == "ACCESS")
            {
                gate.Signal();
                gate.Wait(TimeSpan.FromSeconds(5));
                return TestResponses.Json(HttpStatusCode.Unauthorized, "{\"success\":false}");
            }
            // Once refreshed to ACCESS2 (a valid token) the same call succeeds — as a real server behaves.
            return TestResponses.Ok("{\"success\":true,\"data\":0}");
        });

        // Seed the store so all 5 concurrent callers start with the SAME stale access token.
        store.SetTokens("ACCESS", "REFRESH");

        // Fire on real threads so all 5 can sit in the barrier at once; the synchronous stub would
        // otherwise block the single enumerating thread and the barrier could never fill.
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => Task.Run(async () => await client.GetAsync<int>("/v3/bill/tracking?bill_code=X", default)))
            .ToArray();
        var results = await Task.WhenAll(tasks);

        Assert.True(gate.IsSet); // all 5 stale-token callers reached the barrier concurrently
        Assert.Equal(1, refreshCount); // only one refresh despite 5 concurrent 401s
        Assert.All(results, r => Assert.True(r.IsSuccess));
    }
}
