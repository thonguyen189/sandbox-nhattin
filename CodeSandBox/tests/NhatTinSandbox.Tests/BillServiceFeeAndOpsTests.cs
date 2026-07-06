using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Application.Bills;
using NhatTinSandbox.Infrastructure.Bills;
using NhatTinSandbox.Infrastructure.Persistence;
using Xunit;

namespace NhatTinSandbox.Tests;

public sealed class BillServiceFeeAndOpsTests
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

    private static CreateBillInput SampleInput(string refCode = "TP-001") => new(
        RefCode: refCode, PackageNo: 1, Weight: 2, Width: 0, Length: 0, Height: 0,
        CargoContent: "Hàng", ServiceId: 91, PaymentMethodId: 10, IsReturnDoc: 0,
        CodAmount: 0, Note: null, CargoValue: 0, CargoTypeId: 2,
        SName: "TruePos", SPhone: "0333333333", SAddress: "số 10", SProvinceCode: "01", SWardCode: "00004",
        RName: "A", RPhone: "0911111111", RAddress: "123", RProvinceCode: "79", RWardCode: "27007",
        IsDraft: 0, OtherFee: 0, IsInstallation: 0, BillType: 1, BillReturn: null);

    [Fact]
    public async Task CalcFee_ReturnsAtLeastOneOption_WithPositiveFee()
    {
        using var db = NewDb();
        var svc = new BillService(db);
        var input = new CalcFeeInput(
            PartnerId: 123736, Weight: 1.3, Width: 0, Length: 0, Height: 0,
            ServiceId: null, PaymentMethodId: 10, CodAmount: 120000, CargoValue: 2000000,
            SProvinceId: "79", SWardId: "27007", RProvinceId: "01", RWardId: "00004");

        var options = await svc.CalcFeeAsync(input, CancellationToken.None);

        Assert.NotEmpty(options);
        Assert.All(options, o => Assert.True(o.TotalFee > 0));
    }

    [Fact]
    public async Task Update_ChangesCod_ReturnsUpdatedSummary()
    {
        using var db = NewDb();
        var svc = new BillService(db);
        var created = await svc.CreateAsync(SampleInput(), CancellationToken.None);

        var updated = await svc.UpdateAsync(new UpdateBillInput(
            PartnerId: 123736, BillCode: created.BillCode, CodAmount: 200000,
            CargoValue: null, Weight: null, Length: null, Height: null, Width: null,
            CargoContent: null, ReceiverPhone: null, ReceiverName: null, ReceiverAddress: null,
            PackageNo: null, IsReturnDoc: 0, Note: "updated", IsInstallation: null), CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal(200000, updated!.CodAmount);
    }

    [Fact]
    public async Task Cancel_ReturnsPerCodeResult()
    {
        using var db = NewDb();
        var svc = new BillService(db);
        var created = await svc.CreateAsync(SampleInput(), CancellationToken.None);

        var result = await svc.CancelAsync(new[] { created.BillCode }, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(created.BillCode, result[0].DoCode);
        var bill = db.Bills.Single(b => b.BillCode == created.BillCode);
        Assert.Equal(6, bill.StatusId); // Hủy
    }

    [Fact]
    public async Task Revert_SplitsSuccessAndFailed()
    {
        using var db = NewDb();
        var svc = new BillService(db);
        var created = await svc.CreateAsync(SampleInput(), CancellationToken.None);
        await svc.SetStatusAsync(created.BillCode, 3, "picked", CancellationToken.None); // eligible

        var result = await svc.RevertAsync(new[] { created.BillCode, "CP-UNKNOWN" }, CancellationToken.None);

        Assert.Contains(created.BillCode, result.Success);
        Assert.Contains("CP-UNKNOWN", result.Failed);
    }
}
