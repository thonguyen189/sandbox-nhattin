using Microsoft.AspNetCore.Mvc;
using NhatTinSandbox.Application.Auth;
using NhatTinSandbox.Application.Common;

namespace NhatTinSandbox.Api.Controllers;

[ApiController]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthTokenService _tokens;
    public AuthController(IAuthTokenService tokens) => _tokens = tokens;

    public sealed record SignInBody(string username, string password);
    public sealed record RefreshBody(string refresh_token);

    [HttpPost("/v1/auth/sign-in")]
    public async Task<IActionResult> SignIn([FromBody] SignInBody body, CancellationToken ct)
    {
        var result = await _tokens.SignInAsync(body.username, body.password, ct);
        if (result is null)
            return Unauthorized(ApiResult.Fail("Xác thực thất bại"));
        return Ok(ApiResult.Ok(ToData(result), "Sign in successfully"));
    }

    [HttpPost("/v1/auth/refresh-token")]
    public async Task<IActionResult> Refresh([FromBody] RefreshBody body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.refresh_token))
            return BadRequest(ApiResult.Fail("Thiếu refresh token"));
        var result = await _tokens.RefreshAsync(body.refresh_token, ct);
        if (result is null)
            return Unauthorized(ApiResult.Fail("Token không hợp lệ"));
        return Ok(ApiResult.Ok(ToData(result), "Refresh token successfully"));
    }

    private static object ToData(AuthTokenResult r) => new
    {
        jwt_token = r.JwtToken,
        token_type = r.TokenType,
        token_expires_in = $"{r.TokenExpiresInSeconds}s",
        refresh_token = r.RefreshToken,
        refresh_expires_in = $"{r.RefreshExpiresInSeconds}s"
    };
}
