using NhatTinSandbox.Application.Bills;
using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Infrastructure.Bills;
using NhatTinSandbox.Infrastructure.Persistence;
using Xunit;

namespace NhatTinSandbox.Tests;

// The public create/update/destroy/revert endpoints must auto-emit a webhook on a successful
// state change, mirroring the /sandbox/.../simulate-status behaviour: CANCEL -> status 6 (Hủy)
// and REVERT -> status 9 (Đang chuyển hoàn).
public sealed class BillWebhookOnCancelRevertTests
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
        CargoContent: "Hàng", ServiceId: 91, PaymentMethodId: 10, IsReturnDoc: 0,
        CodAmount: 0, Note: null, CargoValue: 0, CargoTypeId: 2,
        SName: "TruePos", SPhone: "0333333333", SAddress: "số 10", SProvinceCode: "01", SWardCode: "00004",
        RName: "A", RPhone: "0911111111", RAddress: "123", RProvinceCode: "79", RWardCode: "27007",
        IsDraft: 0, OtherFee: 0, IsInstallation: 0, BillType: 1, BillReturn: null);

    [Fact]
    public async Task Cancel_BillInStatus2_DispatchesWebhookWithStatus6()
    {
        using var db = NewDb();
        var dispatcher = new RecordingWebhookDispatcher(db);
        var svc = new BillService(db, dispatcher);
        var created = await svc.CreateAsync(SampleInput(), CancellationToken.None); // status 2
        var billId = db.Bills.Single(b => b.BillCode == created.BillCode).Id;

        await svc.CancelAsync(new[] { created.BillCode }, CancellationToken.None);

        Assert.Contains(dispatcher.Calls, c => c.BillId == billId && c.StatusId == 6);
    }

    [Fact]
    public async Task Cancel_BillNotCancelable_DoesNotDispatch()
    {
        using var db = NewDb();
        var dispatcher = new RecordingWebhookDispatcher(db);
        var svc = new BillService(db, dispatcher);
        var created = await svc.CreateAsync(SampleInput(), CancellationToken.None);
        await svc.SetStatusAsync(created.BillCode, 3, "picked", CancellationToken.None); // not cancelable

        await svc.CancelAsync(new[] { created.BillCode }, CancellationToken.None);

        Assert.Empty(dispatcher.Calls);
    }

    [Fact]
    public async Task Revert_BillInStatus3_DispatchesWebhookWithStatus9()
    {
        using var db = NewDb();
        var dispatcher = new RecordingWebhookDispatcher(db);
        var svc = new BillService(db, dispatcher);
        var created = await svc.CreateAsync(SampleInput(), CancellationToken.None);
        await svc.SetStatusAsync(created.BillCode, 3, "picked", CancellationToken.None); // eligible for revert
        var billId = db.Bills.Single(b => b.BillCode == created.BillCode).Id;

        await svc.RevertAsync(new[] { created.BillCode }, CancellationToken.None);

        Assert.Contains(dispatcher.Calls, c => c.BillId == billId && c.StatusId == 9);
    }

    [Fact]
    public async Task Revert_IneligibleBill_DoesNotDispatch()
    {
        using var db = NewDb();
        var dispatcher = new RecordingWebhookDispatcher(db);
        var svc = new BillService(db, dispatcher);
        var created = await svc.CreateAsync(SampleInput(), CancellationToken.None); // status 2, not revertable

        await svc.RevertAsync(new[] { created.BillCode }, CancellationToken.None);

        Assert.Empty(dispatcher.Calls);
    }
}
