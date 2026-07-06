using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NhatTinLogistics.Sdk.Types.Responses;

namespace NhatTinLogistics.Sdk.Http;

/// <summary>Low-level HTTP layer: token management, snake_case serialization, envelope parsing, 401 retry.</summary>
public sealed class NhatTinHttpClient
{
    private readonly HttpClient _http;
    private readonly NhatTinLogisticsClientOptions _options;
    private readonly ITokenStore _tokens;
    private readonly SemaphoreSlim _authLock = new(1, 1);

    public NhatTinHttpClient(HttpClient http, NhatTinLogisticsClientOptions options, ITokenStore tokens)
    {
        _http = http;
        _options = options;
        _tokens = tokens;
        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(options.ResolveBaseUrl());
    }

    public Task<NhatTinResponse<T>> PostAsync<T>(string path, object body, CancellationToken ct)
        => SendAsync<T>(HttpMethod.Post, path, body, authenticated: true, ct);

    public Task<NhatTinResponse<T>> GetAsync<T>(string path, CancellationToken ct)
        => SendAsync<T>(HttpMethod.Get, path, null, authenticated: true, ct);

    public async Task<NhatTinResponse<T>> SendAsync<T>(
        HttpMethod method, string path, object? body, bool authenticated, CancellationToken ct)
    {
        string? tokenUsed = null;
        if (authenticated)
        {
            await EnsureAuthenticatedAsync(ct).ConfigureAwait(false);
            tokenUsed = _tokens.AccessToken;
        }

        var response = await SendOnceAsync(method, path, body, tokenUsed, ct).ConfigureAwait(false);

        if (authenticated && response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            await RefreshIfStaleAsync(tokenUsed, ct).ConfigureAwait(false);
            tokenUsed = _tokens.AccessToken;
            response = await SendOnceAsync(method, path, body, tokenUsed, ct).ConfigureAwait(false);
        }

        return await ReadResponseAsync<T>(response, ct).ConfigureAwait(false);
    }

    public async Task<byte[]> GetBytesAsync(string url, CancellationToken ct)
    {
        await EnsureAuthenticatedAsync(ct).ConfigureAwait(false);
        var response = await SendOnceAsync(HttpMethod.Get, url, null, _tokens.AccessToken, ct).ConfigureAwait(false);
        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw new NhatTinApiException($"Print request failed. Status {(int)response.StatusCode}.", (int)response.StatusCode);
            return await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        }
    }

    private async Task<HttpResponseMessage> SendOnceAsync(
        HttpMethod method, string path, object? body, string? bearerToken, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, path);
        try
        {
            if (!string.IsNullOrEmpty(bearerToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            if (body is not null)
            {
                var json = JsonSerializer.Serialize(body, body.GetType(), NhatTinJson.Options);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            return await _http.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new NhatTinApiException($"HTTP request to '{path}' failed: {ex.Message}", 0, null, ex);
        }
        finally
        {
            request.Dispose();
        }
    }

    private static async Task<NhatTinResponse<T>> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        using (response)
        {
            var raw = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var status = (int)response.StatusCode;
            RawEnvelope<T>? env;
            try
            {
                env = string.IsNullOrWhiteSpace(raw)
                    ? null
                    : JsonSerializer.Deserialize<RawEnvelope<T>>(raw, NhatTinJson.Options);
            }
            catch (JsonException ex)
            {
                throw new NhatTinApiException($"Failed to parse NhatTin response as JSON. Status {status}.", status, raw, ex);
            }

            if (env is null)
                throw new NhatTinApiException($"Empty NhatTin response. Status {status}.", status, raw);

            return new NhatTinResponse<T>
            {
                Success = env.Success,
                Message = env.Message,
                Data = env.Data,
                HttpStatusCode = status,
                RawBody = raw,
            };
        }
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken ct)
    {
        if (!_options.AutoAuthenticate) return;
        if (!string.IsNullOrEmpty(_tokens.AccessToken)) return;

        await _authLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (string.IsNullOrEmpty(_tokens.AccessToken))
                await SignInAsync(ct).ConfigureAwait(false);
        }
        finally { _authLock.Release(); }
    }

    private async Task RefreshIfStaleAsync(string? staleToken, CancellationToken ct)
    {
        await _authLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Another concurrent caller already refreshed → nothing to do.
            if (!string.IsNullOrEmpty(_tokens.AccessToken) && _tokens.AccessToken != staleToken)
                return;

            var refresh = _tokens.RefreshToken;
            if (!string.IsNullOrEmpty(refresh))
            {
                var res = await SendAsync<AuthToken>(
                    HttpMethod.Post, "/v1/auth/refresh-token", new { refresh_token = refresh }, authenticated: false, ct)
                    .ConfigureAwait(false);
                if (res.IsSuccess && res.Data is not null && !string.IsNullOrEmpty(res.Data.JwtToken))
                {
                    _tokens.SetTokens(res.Data.JwtToken, res.Data.RefreshToken);
                    return;
                }
            }
            // No refresh token or refresh failed → full sign-in.
            _tokens.Clear();
            await SignInAsync(ct).ConfigureAwait(false);
        }
        finally { _authLock.Release(); }
    }

    private async Task SignInAsync(CancellationToken ct)
    {
        var res = await SendAsync<AuthToken>(
            HttpMethod.Post, "/v1/auth/sign-in",
            new { username = _options.Username, password = _options.Password },
            authenticated: false, ct).ConfigureAwait(false);

        if (!res.IsSuccess || res.Data is null || string.IsNullOrEmpty(res.Data.JwtToken))
            throw new NhatTinApiException($"Sign-in failed: {res.Message}", res.HttpStatusCode, res.RawBody);

        _tokens.SetTokens(res.Data.JwtToken, res.Data.RefreshToken);
    }
}
