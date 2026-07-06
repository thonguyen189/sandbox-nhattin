namespace NhatTinSandbox.Application.Auth;

public sealed record SignInInput(string Username, string Password);

public sealed record AuthTokenResult(
    string JwtToken,
    string TokenType,
    int TokenExpiresInSeconds,
    string RefreshToken,
    int RefreshExpiresInSeconds);

public interface IAuthTokenService
{
    // Returns null when credentials are invalid.
    Task<AuthTokenResult?> SignInAsync(string username, string password, CancellationToken ct);
    // Returns null when the refresh token is missing/expired/revoked.
    Task<AuthTokenResult?> RefreshAsync(string refreshToken, CancellationToken ct);
}
