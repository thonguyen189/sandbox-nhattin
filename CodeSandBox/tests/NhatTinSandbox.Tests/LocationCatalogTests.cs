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
}
