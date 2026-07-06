namespace NhatTinLogistics.Sdk.Http;

/// <summary>Holds the current JWT access + refresh tokens. Default impl is in-memory; consumers may supply their own.</summary>
public interface ITokenStore
{
    string? AccessToken { get; }
    string? RefreshToken { get; }
    void SetTokens(string accessToken, string refreshToken);
    void Clear();
}
