using System.Net.Http;
using NhatTinLogistics.Sdk;
using NhatTinLogistics.Sdk.Client;
using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Tests.Infrastructure;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class AuthApiTests
{
    [Fact]
    public async Task SignInAsync_posts_credentials_and_maps_token()
    {
        var handler = new StubHttpMessageHandler(_ =>
            TestResponses.Ok("{\"success\":true,\"data\":{\"jwt_token\":\"A\",\"refresh_token\":\"R\",\"token_type\":\"Bearer\"}}"));
        var options = new NhatTinLogisticsClientOptions { AutoAuthenticate = false, BaseUrl = "https://test.local" };
        var http = new HttpClient(handler) { BaseAddress = new Uri(options.ResolveBaseUrl()) };
        var api = new AuthApi(new NhatTinHttpClient(http, options, new InMemoryTokenStore()));

        var resp = await api.SignInAsync("john", "secret");

        Assert.True(resp.IsSuccess);
        Assert.Equal("A", resp.Data!.JwtToken);
        Assert.Equal("R", resp.Data.RefreshToken);
        Assert.EndsWith("/v1/auth/sign-in", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Contains("\"username\":\"john\"", handler.RequestBodies[0]);
        Assert.Contains("\"password\":\"secret\"", handler.RequestBodies[0]);
    }
}
