using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Domain.Entities;

namespace NhatTinSandbox.Infrastructure.Persistence;

public class SandboxDbContext : DbContext
{
    public SandboxDbContext(DbContextOptions<SandboxDbContext> options) : base(options) { }

    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<BillStatusHistory> BillStatusHistories => Set<BillStatusHistory>();
    public DbSet<PartnerAccount> PartnerAccounts => Set<PartnerAccount>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDeliveryLog> WebhookDeliveryLogs => Set<WebhookDeliveryLog>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<MasterDataItem> MasterData => Set<MasterDataItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Bill>().HasIndex(x => x.BillCode).IsUnique();
        b.Entity<Bill>().Property(x => x.CodAmount).HasPrecision(18, 2);
        b.Entity<Bill>().Property(x => x.CargoValue);
        b.Entity<Bill>().Property(x => x.MainFee).HasPrecision(18, 2);
        b.Entity<Bill>().Property(x => x.TotalFee).HasPrecision(18, 2);
        b.Entity<Bill>().Property(x => x.OtherFee).HasPrecision(18, 2);
        b.Entity<BillStatusHistory>().Property(x => x.ShippingFee).HasPrecision(18, 2);
        b.Entity<WebhookDeliveryLog>().HasIndex(x => x.BillCode);
        b.Entity<Location>().HasIndex(x => new { x.Kind, x.Code });
        b.Entity<MasterDataItem>().HasIndex(x => new { x.Kind, x.Code });
    }
}
