using System;

namespace NhatTinLogistics.Sdk.Http;

/// <summary>Holds the current JWT access + refresh tokens. Default impl is in-memory; consumers may supply their own.</summary>
public interface ITokenStore
{
    string? AccessToken { get; }
    string? RefreshToken { get; }

    /// <summary>Absolute UTC instant the access token expires, if known. Enables proactive refresh; null disables it.</summary>
    DateTimeOffset? AccessTokenExpiresAt { get; }

    /// <summary>Absolute UTC instant the refresh token expires, if known.</summary>
    DateTimeOffset? RefreshTokenExpiresAt { get; }

    /// <summary>
    /// Stores the tokens and their optional expiry instants. The expiry parameters default to null so existing
    /// two-argument callers keep compiling; pass them to enable proactive refresh.
    /// </summary>
    void SetTokens(string accessToken, string refreshToken,
                   DateTimeOffset? accessExpiresAt = null,
                   DateTimeOffset? refreshExpiresAt = null);

    void Clear();
}
