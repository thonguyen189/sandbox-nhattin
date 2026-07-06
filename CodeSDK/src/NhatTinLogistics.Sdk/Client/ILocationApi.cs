using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Types.Responses;

namespace NhatTinLogistics.Sdk.Client;

public interface ILocationApi
{
    Task<NhatTinResponse<List<ProvinceDto>>> GetProvincesAsync(bool isNew, CancellationToken ct = default);
    Task<NhatTinResponse<List<DistrictDto>>> GetDistrictsAsync(string provinceId, CancellationToken ct = default);
    Task<NhatTinResponse<List<WardDto>>> GetWardsAsync(string? districtId, string? provinceId, bool isNew, CancellationToken ct = default);
}
