namespace NhatTinSandbox.Application.Locations;

public sealed record ProvinceDto(string Id, string ProvinceName, string IsNew);
public sealed record DistrictDto(string Id, string DistrictName, string IsNew);
public sealed record WardDto(string Id, string WardName, string IsNew);

public interface ILocationCatalog
{
    Task<IReadOnlyList<ProvinceDto>> GetProvincesAsync(bool isNew, CancellationToken ct);
    Task<IReadOnlyList<DistrictDto>> GetDistrictsAsync(string provinceId, CancellationToken ct);
    Task<IReadOnlyList<WardDto>> GetWardsAsync(string? districtId, string? provinceId, bool isNew, CancellationToken ct);
}
