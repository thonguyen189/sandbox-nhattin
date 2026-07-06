using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Types.Responses;

namespace NhatTinLogistics.Sdk.Client;

public sealed class LocationApi : ILocationApi
{
    private readonly NhatTinHttpClient _http;
    public LocationApi(NhatTinHttpClient http) => _http = http;

    public Task<NhatTinResponse<List<ProvinceDto>>> GetProvincesAsync(bool isNew, CancellationToken ct = default)
        => _http.GetAsync<List<ProvinceDto>>($"/v3/loc/provinces?is_new={(isNew ? 1 : 0)}", ct);

    public Task<NhatTinResponse<List<DistrictDto>>> GetDistrictsAsync(string provinceId, CancellationToken ct = default)
        => _http.GetAsync<List<DistrictDto>>($"/v3/loc/districts?province_id={Uri.EscapeDataString(provinceId)}", ct);

    public Task<NhatTinResponse<List<WardDto>>> GetWardsAsync(string? districtId, string? provinceId, bool isNew, CancellationToken ct = default)
    {
        var q = new List<string> { $"is_new={(isNew ? 1 : 0)}" };
        if (!string.IsNullOrEmpty(districtId)) q.Add($"district_id={Uri.EscapeDataString(districtId)}");
        if (!string.IsNullOrEmpty(provinceId)) q.Add($"province_id={Uri.EscapeDataString(provinceId)}");
        return _http.GetAsync<List<WardDto>>($"/v3/loc/wards?{string.Join("&", q)}", ct);
    }
}
