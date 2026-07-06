using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Domain.Entities;
using NhatTinSandbox.Infrastructure.Persistence;
using Xunit;

namespace NhatTinSandbox.Tests;

public sealed class SeedDataTests
{
    private static SandboxDbContext NewInMemorySqlite()
    {
        var conn = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<SandboxDbContext>().UseSqlite(conn).Options;
        var db = new SandboxDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public void EnsureSeeded_IsIdempotent_AndSeedsDemoAccount()
    {
        using var db = NewInMemorySqlite();

        SeedData.EnsureSeeded(db);
        SeedData.EnsureSeeded(db); // second call must not duplicate

        Assert.Single(db.PartnerAccounts);
        Assert.Equal("sandbox", db.PartnerAccounts.Single().Username);
        Assert.Single(db.WebhookSubscriptions);
        Assert.Contains(db.Locations, l => l.Kind == LocationKind.Province && l.Code == "79");
        Assert.Contains(db.MasterData, m => m.Kind == MasterDataKind.Service && m.Code == 91);
    }
}
