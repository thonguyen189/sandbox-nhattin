using System.Net.Http;
using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Types.Responses;

namespace NhatTinLogistics.Sdk.Client;

public sealed class AuthApi : IAuthApi
{
    private readonly NhatTinHttpClient _http;
    public AuthApi(NhatTinHttpClient http) => _http = http;

    public Task<NhatTinResponse<AuthToken>> SignInAsync(string username, string password, CancellationToken ct = default)
        => _http.SendAsync<AuthToken>(HttpMethod.Post, "/v1/auth/sign-in",
            new { username, password }, authenticated: false, ct);

    public Task<NhatTinResponse<AuthToken>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
        => _http.SendAsync<AuthToken>(HttpMethod.Post, "/v1/auth/refresh-token",
            new { refresh_token = refreshToken }, authenticated: false, ct);
}
