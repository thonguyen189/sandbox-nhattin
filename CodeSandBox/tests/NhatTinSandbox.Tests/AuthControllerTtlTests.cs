using Microsoft.AspNetCore.Mvc;
using NhatTinSandbox.Api.Controllers;
using NhatTinSandbox.Application.Auth;
using Xunit;

namespace NhatTinSandbox.Tests;

// Ground truth (verified live): the real Nhất Tín login returns human-unit TTL strings
// token_expires_in = "24h" and refresh_expires_in = "7d". The controller humanizes the
// configured seconds (access -> hours, refresh -> days).
public sealed class AuthControllerTtlTests
{
    private sealed class FakeTokenService : IAuthTokenService
    {
        private readonly AuthTokenResult _result;
        public FakeTokenService(AuthTokenResult result) => _result = result;
        public Task<AuthTokenResult?> SignInAsync(string username, string password, CancellationToken ct)
            => Task.FromResult<AuthTokenResult?>(_result);
        public Task<AuthTokenResult?> RefreshAsync(string refreshToken, CancellationToken ct)
            => Task.FromResult<AuthTokenResult?>(_result);
    }

    private static (string tokenExpiresIn, string refreshExpiresIn) ReadTtls(IActionResult actionResult)
    {
        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var envelope = ok.Value!;
        var data = envelope.GetType().GetProperty("data")!.GetValue(envelope)!;
        var tokenExpiresIn = (string)data.GetType().GetProperty("token_expires_in")!.GetValue(data)!;
        var refreshExpiresIn = (string)data.GetType().GetProperty("refresh_expires_in")!.GetValue(data)!;
        return (tokenExpiresIn, refreshExpiresIn);
    }

    [Fact]
    public async Task SignIn_Humanizes_24h_And_7d()
    {
        var result = new AuthTokenResult("jwt", "Bearer", 86400, "refresh", 604800);
        var controller = new AuthController(new FakeTokenService(result));

        var action = await controller.SignIn(new AuthController.SignInBody("sandbox", "sandbox123"), CancellationToken.None);

        var (token, refresh) = ReadTtls(action);
        Assert.Equal("24h", token);
        Assert.Equal("7d", refresh);
    }

    [Fact]
    public async Task Refresh_Humanizes_24h_And_7d()
    {
        var result = new AuthTokenResult("jwt", "Bearer", 86400, "refresh", 604800);
        var controller = new AuthController(new FakeTokenService(result));

        var action = await controller.Refresh(new AuthController.RefreshBody("some-refresh-token"), CancellationToken.None);

        var (token, refresh) = ReadTtls(action);
        Assert.Equal("24h", token);
        Assert.Equal("7d", refresh);
    }
}
