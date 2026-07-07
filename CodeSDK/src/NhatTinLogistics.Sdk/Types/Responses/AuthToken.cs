using System.Text.Json.Serialization;

namespace NhatTinLogistics.Sdk.Types.Responses;

public sealed class AuthToken
{
    [JsonPropertyName("jwt_token")] public string JwtToken { get; set; } = "";
    [JsonPropertyName("token_type")] public string? TokenType { get; set; }
    [JsonPropertyName("token_expires_in")] public string? TokenExpiresIn { get; set; }
    [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = "";
    [JsonPropertyName("refresh_expires_in")] public string? RefreshExpiresIn { get; set; }
    [JsonPropertyName("partner_id")] public int? PartnerId { get; set; }
}
