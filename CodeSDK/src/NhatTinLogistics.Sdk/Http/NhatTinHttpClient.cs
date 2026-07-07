using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NhatTinLogistics.Sdk.Types.Responses;

namespace NhatTinLogistics.Sdk.Http;

/// <summary>
/// Low-level HTTP layer: token management (proactive refresh + reactive 401 refresh), snake_case
/// serialization, envelope parsing, and transient-fault retry with backoff for idempotent calls.
/// </summary>
public sealed class NhatTinHttpClient
{
    private readonly HttpClient _http;
    private readonly NhatTinLogisticsClientOptions _options;
    private readonly ITokenStore _tokens;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly SemaphoreSlim _authLock = new(1, 1);

    /// <summary>Idempotent POST paths that are safe to retry (unlike the bill write endpoints).</summary>
    private static readonly HashSet<string> IdempotentPostPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/v3/bill/calc-fee",
        "/v1/auth/sign-in",
        "/v1/auth/refresh-token",
    };

    /// <summary>
    /// <paramref name="clock"/> is an injectable time source (default UTC now) so tests can drive token expiry
    /// deterministically; <paramref name="delay"/> is an injectable backoff sleep (default <see cref="Task.Delay(TimeSpan, CancellationToken)"/>)
    /// so tests can pass a no-op. Both default to production behavior.
    /// </summary>
    public NhatTinHttpClient(HttpClient http, NhatTinLogisticsClientOptions options, ITokenStore tokens,
        Func<DateTimeOffset>? clock = null, Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _http = http;
        _options = options;
        _tokens = tokens;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _delay = delay ?? ((ts, c) => Task.Delay(ts, c));
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
        var response = await ExecuteWithRetryAsync(
            () => SendWithAuthAsync(method, path, body, authenticated, ct),
            IsIdempotent(method, path), ct).ConfigureAwait(false);
        return await ReadResponseAsync<T>(response, ct).ConfigureAwait(false);
    }

    /// <summary>Ensures auth (when required), sends once, and performs the single 401 refresh-and-retry.</summary>
    private async Task<HttpResponseMessage> SendWithAuthAsync(
        HttpMethod method, string path, object? body, bool authenticated, CancellationToken ct)
    {
        string? tokenUsed = null;
        if (authenticated)
        {
            await EnsureAuthenticatedAsync(ct).ConfigureAwait(false);
            tokenUsed = _tokens.AccessToken;
        }

        var response = await SendOnceAsync(method, path, body, tokenUsed, ct).ConfigureAwait(false);

        // Only refresh-and-retry when the SDK owns auth. With AutoAuthenticate=false the caller
        // manages tokens: attach whatever is seeded and return the raw response (a 401 → IsSuccess=false).
        if (authenticated && _options.AutoAuthenticate && response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            await RefreshIfStaleAsync(tokenUsed, ct).ConfigureAwait(false);
            tokenUsed = _tokens.AccessToken;
            response = await SendOnceAsync(method, path, body, tokenUsed, ct).ConfigureAwait(false);
        }

        return response;
    }

    /// <summary>
    /// Fetches a print response as raw bytes + content-type, using the same auth flow as <see cref="SendAsync{T}"/>
    /// (ensure-authenticated, single 401 refresh-and-retry when AutoAuthenticate). NhatTin returns HTTP 200 for
    /// both success (HTML) and business errors (a JSON envelope), so this never throws on an HTTP-error envelope;
    /// only transport/network failures surface as <see cref="NhatTinApiException"/>. The caller inspects
    /// <see cref="PrintResult.Success"/>.
    /// </summary>
    public async Task<PrintResult> GetPrintAsync(string url, CancellationToken ct)
    {
        // Print is a GET → idempotent → eligible for transient retry. SendWithAuthAsync handles
        // ensure-auth + the single 401 refresh-and-retry, exactly as the old inline flow did.
        var response = await ExecuteWithRetryAsync(
            () => SendWithAuthAsync(HttpMethod.Get, url, null, authenticated: true, ct),
            idempotent: true, ct).ConfigureAwait(false);

        using (response)
        {
            var status = (int)response.StatusCode;
            var contentType = response.Content.Headers.ContentType?.ToString();
            var bytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
            return new PrintResult(status, contentType, bytes);
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

    /// <summary>GET is always idempotent; only an allowlist of POST paths is (calc-fee / sign-in / refresh).</summary>
    private static bool IsIdempotent(HttpMethod method, string path)
    {
        if (method == HttpMethod.Get) return true;
        if (method == HttpMethod.Post)
        {
            var q = path.IndexOf('?');
            var pathOnly = q >= 0 ? path.Substring(0, q) : path;
            return IdempotentPostPaths.Contains(pathOnly);
        }
        return false;
    }

    private static bool IsTransientStatus(HttpStatusCode status)
        => (int)status >= 500                             // 5xx server errors
           || status == HttpStatusCode.RequestTimeout     // 408
           || status == HttpStatusCode.TooManyRequests;   // 429

    /// <summary>
    /// Runs <paramref name="sendUnit"/> and retries it on transient failures (transport errors, timeouts,
    /// HTTP 5xx/429/408) with exponential backoff + jitter — but only for idempotent calls, and only while
    /// retries remain. Non-idempotent calls run exactly once. The final attempt's outcome (response or
    /// exception) is surfaced unchanged.
    /// </summary>
    private async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<Task<HttpResponseMessage>> sendUnit, bool idempotent, CancellationToken ct)
    {
        var maxAttempts = _options.EnableRetry && idempotent
            ? Math.Max(0, _options.MaxRetries) + 1
            : 1;

        for (var attempt = 1; ; attempt++)
        {
            HttpResponseMessage response;
            try
            {
                response = await sendUnit().ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested && attempt < maxAttempts)
            {
                // A client-side timeout surfaces as OperationCanceledException without ct being cancelled.
                await BackoffAsync(attempt, ct).ConfigureAwait(false);
                continue;
            }
            catch (NhatTinApiException ex) when (ex.HttpStatusCode == 0 && attempt < maxAttempts)
            {
                // Transport failure (DNS/socket/connection reset) wrapped by SendOnceAsync.
                await BackoffAsync(attempt, ct).ConfigureAwait(false);
                continue;
            }

            if (attempt < maxAttempts && IsTransientStatus(response.StatusCode))
            {
                response.Dispose();
                await BackoffAsync(attempt, ct).ConfigureAwait(false);
                continue;
            }

            return response;
        }
    }

    private async Task BackoffAsync(int attempt, CancellationToken ct)
    {
        var baseMs = _options.RetryBaseDelay.TotalMilliseconds;
        var expMs = baseMs * Math.Pow(2, attempt - 1);        // attempt 1 → base, 2 → 2x, 3 → 4x …
        var capMs = Math.Min(expMs, _options.RetryMaxDelay.TotalMilliseconds);
        // Equal jitter (half fixed + half random) spreads out concurrent retries without collapsing to ~0.
        var jitteredMs = capMs / 2 + Random.Shared.NextDouble() * (capMs / 2);
        await _delay(TimeSpan.FromMilliseconds(jitteredMs), ct).ConfigureAwait(false);
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken ct)
    {
        if (!_options.AutoAuthenticate) return;
        if (IsAccessTokenFresh()) return;

        await _authLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Re-check after acquiring the lock: another caller may have just refreshed/signed in.
            if (IsAccessTokenFresh()) return;

            // Access token missing or within the expiry skew. Prefer a refresh while the refresh token
            // is still usable; otherwise fall back to a full sign-in.
            if (IsRefreshTokenUsable() && await TryRefreshAsync(ct).ConfigureAwait(false))
                return;

            _tokens.Clear();
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

            if (IsRefreshTokenUsable() && await TryRefreshAsync(ct).ConfigureAwait(false))
                return;

            // No usable refresh token or refresh failed → full sign-in.
            _tokens.Clear();
            await SignInAsync(ct).ConfigureAwait(false);
        }
        finally { _authLock.Release(); }
    }

    /// <summary>True when we hold an access token that is present and not within the proactive-refresh skew.</summary>
    private bool IsAccessTokenFresh()
    {
        if (string.IsNullOrEmpty(_tokens.AccessToken)) return false;
        if (!_options.EnableProactiveRefresh) return true;   // reactive-only: any present token counts as usable
        var exp = _tokens.AccessTokenExpiresAt;
        if (exp is null) return true;                         // unknown TTL → rely on the 401 path
        return _clock() < exp.Value - _options.TokenExpirySkew;
    }

    private bool IsRefreshTokenUsable()
    {
        if (string.IsNullOrEmpty(_tokens.RefreshToken)) return false;
        var exp = _tokens.RefreshTokenExpiresAt;
        if (exp is null) return true;                         // unknown TTL → try it; let the server reject
        return _clock() < exp.Value;                          // no skew: usable until the actual expiry
    }

    /// <summary>Performs one refresh-token call and stores the result. Returns false without throwing on failure.</summary>
    private async Task<bool> TryRefreshAsync(CancellationToken ct)
    {
        var refresh = _tokens.RefreshToken;
        if (string.IsNullOrEmpty(refresh)) return false;

        var res = await SendAsync<AuthToken>(
            HttpMethod.Post, "/v1/auth/refresh-token", new { refresh_token = refresh }, authenticated: false, ct)
            .ConfigureAwait(false);
        if (res.IsSuccess && res.Data is not null && !string.IsNullOrEmpty(res.Data.JwtToken))
        {
            StoreTokens(res.Data, isRefresh: true);
            return true;
        }
        return false;
    }

    private async Task SignInAsync(CancellationToken ct)
    {
        var res = await SendAsync<AuthToken>(
            HttpMethod.Post, "/v1/auth/sign-in",
            new { username = _options.Username, password = _options.Password },
            authenticated: false, ct).ConfigureAwait(false);

        if (!res.IsSuccess || res.Data is null || string.IsNullOrEmpty(res.Data.JwtToken))
            throw new NhatTinApiException($"Sign-in failed: {res.Message}", res.HttpStatusCode, res.RawBody);

        StoreTokens(res.Data, isRefresh: false);

        // The sign-in envelope now carries partner_id; adopt it as the default unless the caller set one.
        if (_options.PartnerId is null && res.Data.PartnerId is not null)
            _options.PartnerId = res.Data.PartnerId;
    }

    /// <summary>
    /// Stores tokens from an auth response, computing absolute expiry from the TTL strings when present.
    /// On a refresh that omits refresh_token, the previously held refresh token and its expiry are kept.
    /// </summary>
    private void StoreTokens(AuthToken data, bool isRefresh)
    {
        var now = _clock();
        DateTimeOffset? accessExp = TokenTtl.Parse(data.TokenExpiresIn) is { } at ? now + at : null;

        string refreshToken;
        DateTimeOffset? refreshExp;
        if (isRefresh && string.IsNullOrEmpty(data.RefreshToken))
        {
            // Kept the existing refresh token → keep its existing expiry too.
            refreshToken = _tokens.RefreshToken ?? "";
            refreshExp = _tokens.RefreshTokenExpiresAt;
        }
        else
        {
            refreshToken = data.RefreshToken;
            refreshExp = TokenTtl.Parse(data.RefreshExpiresIn) is { } rt ? now + rt : null;
        }

        _tokens.SetTokens(data.JwtToken, refreshToken, accessExp, refreshExp);
    }
}
