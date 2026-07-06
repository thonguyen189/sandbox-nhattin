using System.Text.Json.Serialization;

namespace NhatTinLogistics.Sdk.Types.Responses;

public sealed class ProvinceDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("province_name")] public string? ProvinceName { get; set; }
    [JsonPropertyName("is_new")] public string? IsNew { get; set; }
}

public sealed class DistrictDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("district_name")] public string? DistrictName { get; set; }
    [JsonPropertyName("is_new")] public string? IsNew { get; set; }
}

public sealed class WardDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("ward_name")] public string? WardName { get; set; }
    [JsonPropertyName("is_new")] public string? IsNew { get; set; }
}
