using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Application.Bills;
using NhatTinSandbox.Infrastructure.Bills;
using NhatTinSandbox.Infrastructure.Persistence;
using Xunit;

namespace NhatTinSandbox.Tests;

public sealed class BillServiceTests
{
    private static SandboxDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<SandboxDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new SandboxDbContext(options);
        db.Database.EnsureCreated();
        SeedData.EnsureSeeded(db);
        return db;
    }

    private static CreateBillInput SampleInput() => new(
        RefCode: "TP-001", PackageNo: 1, Weight: 2, Width: 0, Length: 0, Height: 0,
        CargoContent: "Hàng dễ vỡ", ServiceId: 91, PaymentMethodId: 10, IsReturnDoc: 0,
        CodAmount: 120000, Note: null, CargoValue: 0, CargoTypeId: 2,
        SName: "TruePos", SPhone: "0333333333", SAddress: "số 10", SProvinceCode: "01", SWardCode: "00004",
        RName: "Nguyễn Văn A", RPhone: "0911111111", RAddress: "123", RProvinceCode: "79", RWardCode: "27007",
        IsDraft: 0, OtherFee: 0, IsInstallation: 0, BillType: 1, BillReturn: null);

    [Fact]
    public async Task Create_AssignsBillCodeStartingWithCP_AndStatus2()
    {
        using var db = NewDb();
        var svc = new BillService(db);

        var summary = await svc.CreateAsync(SampleInput(), CancellationToken.None);

        Assert.StartsWith("CP", summary.BillCode);
        Assert.Equal(2, summary.StatusId);
        Assert.Equal("TP-001", summary.RefCode);
        Assert.True(summary.TotalFee > 0);
    }

    [Fact]
    public async Task Create_DraftBill_StartsAtStatus12()
    {
        using var db = NewDb();
        var svc = new BillService(db);

        var draft = SampleInput() with { IsDraft = 1 };
        var summary = await svc.CreateAsync(draft, CancellationToken.None);

        Assert.Equal(12, summary.StatusId);
    }

    [Fact]
    public async Task GetByCode_ReturnsCreatedBill()
    {
        using var db = NewDb();
        var svc = new BillService(db);
        var created = await svc.CreateAsync(SampleInput(), CancellationToken.None);

        var found = await svc.GetByCodeAsync(created.BillCode, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(created.BillCode, found!.BillCode);
    }

    [Fact]
    public async Task SetStatus_ChangesStatus_AndRecordsHistory()
    {
        using var db = NewDb();
        var svc = new BillService(db);
        var created = await svc.CreateAsync(SampleInput(), CancellationToken.None);

        var updated = await svc.SetStatusAsync(created.BillCode, 3, "Đã lấy hàng", CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal(3, updated!.StatusId);
        var bill = db.Bills.Single(x => x.BillCode == created.BillCode);
        Assert.Contains(db.BillStatusHistories, h => h.BillId == bill.Id && h.StatusId == 3);
    }
}
