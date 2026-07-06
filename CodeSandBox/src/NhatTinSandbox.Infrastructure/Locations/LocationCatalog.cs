using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Application.Locations;
using NhatTinSandbox.Domain.Entities;
using NhatTinSandbox.Infrastructure.Persistence;

namespace NhatTinSandbox.Infrastructure.Locations;

public sealed class LocationCatalog : ILocationCatalog
{
    private readonly SandboxDbContext _db;
    public LocationCatalog(SandboxDbContext db) => _db = db;

    private static string Flag(bool isNew) => isNew ? "Y" : "N";

    public async Task<IReadOnlyList<ProvinceDto>> GetProvincesAsync(bool isNew, CancellationToken ct)
    {
        var rows = await _db.Locations
            .Where(l => l.Kind == LocationKind.Province && l.IsNew == isNew)
            .OrderBy(l => l.Code)
            .ToListAsync(ct);
        return rows.Select(l => new ProvinceDto(l.Code, l.Name, Flag(l.IsNew))).ToList();
    }

    public async Task<IReadOnlyList<DistrictDto>> GetDistrictsAsync(string provinceId, CancellationToken ct)
    {
        var rows = await _db.Locations
            .Where(l => l.Kind == LocationKind.District && l.ParentCode == provinceId)
            .OrderBy(l => l.Code)
            .ToListAsync(ct);
        return rows.Select(l => new DistrictDto(l.Code, l.Name, Flag(l.IsNew))).ToList();
    }

    public async Task<IReadOnlyList<WardDto>> GetWardsAsync(string? districtId, string? provinceId, bool isNew, CancellationToken ct)
    {
        var query = _db.Locations.Where(l => l.Kind == LocationKind.Ward && l.IsNew == isNew);
        if (!string.IsNullOrEmpty(districtId))
            query = query.Where(l => l.DistrictCode == districtId);
        if (!string.IsNullOrEmpty(provinceId))
            query = query.Where(l => l.ParentCode == provinceId);

        var rows = await query.OrderBy(l => l.Code).ToListAsync(ct);
        return rows.Select(l => new WardDto(l.Code, l.Name, Flag(l.IsNew))).ToList();
    }
}
