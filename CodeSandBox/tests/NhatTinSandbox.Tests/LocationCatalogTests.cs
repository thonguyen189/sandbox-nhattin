using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Infrastructure.Locations;
using NhatTinSandbox.Infrastructure.Persistence;
using Xunit;

namespace NhatTinSandbox.Tests;

public sealed class LocationCatalogTests
{
    private static SandboxDbContext NewDb()
    {
        var conn = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<SandboxDbContext>().UseSqlite(conn).Options;
        var db = new SandboxDbContext(options);
        db.Database.EnsureCreated();
        SeedData.EnsureSeeded(db);
        return db;
    }

    [Fact]
    public async Task GetProvinces_ReturnsSeededProvinces_WithIsNewFlag()
    {
        using var db = NewDb();
        var catalog = new LocationCatalog(db);

        var provinces = await catalog.GetProvincesAsync(isNew: true, CancellationToken.None);

        Assert.Contains(provinces, p => p.Id == "79" && p.ProvinceName == "Hồ Chí Minh" && p.IsNew == "Y");
    }

    [Fact]
    public async Task GetWards_ByProvince_FiltersByProvinceCode()
    {
        using var db = NewDb();
        var catalog = new LocationCatalog(db);

        var wards = await catalog.GetWardsAsync(districtId: null, provinceId: "79", isNew: true, CancellationToken.None);

        Assert.All(wards, w => Assert.False(string.IsNullOrEmpty(w.WardName)));
        Assert.Contains(wards, w => w.Id == "27007");
    }

    [Fact]
    public async Task GetProvinces_WithIsNewFalse_ReturnsOldUnitSeed()
    {
        // provinces.md: is_new defaults to 0 (old-unit). A first-time integrator following the
        // doc's default must get at least one result, not an empty list.
        using var db = NewDb();
        var catalog = new LocationCatalog(db);

        var provinces = await catalog.GetProvincesAsync(isNew: false, CancellationToken.None);

        Assert.NotEmpty(provinces);
        Assert.Contains(provinces, p => p.IsNew == "N");
    }

    [Fact]
    public async Task GetWards_WithIsNewTrue_StillReturnsSeededNewWard()
    {
        using var db = NewDb();
        var catalog = new LocationCatalog(db);

        var wards = await catalog.GetWardsAsync(districtId: null, provinceId: "79", isNew: true, CancellationToken.None);

        Assert.Contains(wards, w => w.Id == "27007");
    }

    [Fact]
    public async Task GetWards_WithIsNewFalse_DoesNotReturnNewUnitWard()
    {
        using var db = NewDb();
        var catalog = new LocationCatalog(db);

        var wards = await catalog.GetWardsAsync(districtId: null, provinceId: "79", isNew: false, CancellationToken.None);

        Assert.DoesNotContain(wards, w => w.Id == "27007");
    }
}
