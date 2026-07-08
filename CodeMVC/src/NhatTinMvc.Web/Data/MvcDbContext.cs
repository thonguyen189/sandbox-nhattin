using Microsoft.EntityFrameworkCore;
using NhatTinMvc.Web.Data.Entities;

namespace NhatTinMvc.Web.Data;

public class MvcDbContext : DbContext
{
    public MvcDbContext(DbContextOptions<MvcDbContext> options) : base(options) { }

    public DbSet<TrackedBill> TrackedBills => Set<TrackedBill>();
    public DbSet<BillStatusEvent> BillStatusEvents => Set<BillStatusEvent>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<TrackedBill>(e =>
        {
            e.HasIndex(x => x.BillCode).IsUnique();
            e.Property(x => x.BillCode).HasMaxLength(64);
            e.Property(x => x.TotalFee).HasColumnType("decimal(18,2)");
        });

        b.Entity<BillStatusEvent>(e =>
        {
            // Self-dedupe: filtered unique index (SQL Server). Chống 2 hàng cùng key khi webhook lặp.
            e.HasIndex(x => x.DedupeKey).IsUnique().HasFilter("[DedupeKey] IS NOT NULL");
            e.Property(x => x.BillCode).HasMaxLength(64);
            e.Property(x => x.DedupeKey).HasMaxLength(160);
            e.HasOne(x => x.TrackedBill)
                .WithMany(x => x.Events)
                .HasForeignKey(x => x.TrackedBillId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
