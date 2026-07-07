using System.Net;
using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Application.Bills;
using NhatTinSandbox.Domain.Entities;
using NhatTinSandbox.Infrastructure.Bills;
using NhatTinSandbox.Infrastructure.Persistence;
using NhatTinSandbox.Infrastructure.Webhooks;
using Xunit;

namespace NhatTinSandbox.Tests;

public sealed class WebhookDispatcherTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public string? LastBody;
        public HttpStatusCode Status = HttpStatusCode.OK;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(Status) { Content = new StringContent("{\"success\":true}") };
        }
    }

    private sealed class StubFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public StubFactory(HttpMessageHandler h) => _handler = h;
        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }

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
        RefCode: "TP-001", PackageNo: 1, Weight: 2, Width: 1, Length: 1, Height: 1,
        CargoContent: "Hàng", ServiceId: 91, PaymentMethodId: 10, IsReturnDoc: 0,
        CodAmount: 0, Note: null, CargoValue: 0, CargoTypeId: 2,
        SName: "TruePos", SPhone: "0333333333", SAddress: "số 10", SProvinceCode: "01", SWardCode: "00004",
        RName: "A", RPhone: "0911111111", RAddress: "123", RProvinceCode: "79", RWardCode: "27007",
        IsDraft: 0, OtherFee: 0, IsInstallation: 0, BillType: 1, BillReturn: null);

    [Fact]
    public async Task Dispatch_PostsPayloadWithBillNo_AndLogsSuccess()
    {
        using var db = NewDb();
        var billSvc = new BillService(db, new RecordingWebhookDispatcher());
        var bill = await billSvc.CreateAsync(SampleInput(), CancellationToken.None);
        await billSvc.SetStatusAsync(bill.BillCode, 3, "Đã lấy hàng", CancellationToken.None);
        var billEntity = db.Bills.Single(b => b.BillCode == bill.BillCode);

        var handler = new StubHandler();
        var dispatcher = new HttpWebhookDispatcher(db, new StubFactory(handler));

        await dispatcher.DispatchAsync(billEntity.Id, CancellationToken.None);

        Assert.NotNull(handler.LastBody);
        Assert.Contains("\"bill_no\"", handler.LastBody);
        Assert.Contains(bill.BillCode, handler.LastBody);
        Assert.Contains(db.WebhookDeliveryLogs, l => l.BillCode == bill.BillCode && l.Success);
    }

    [Fact]
    public async Task Dispatch_DeliversToActiveSubscription_WithDifferentPartnerId()
    {
        using var db = NewDb();
        // Seeded subscription is PartnerId 123736; add one for a different partner (e.g. added via AdminPortal).
        db.WebhookSubscriptions.Add(new WebhookSubscription
        {
            PartnerId = 999999,
            CallbackUrl = "http://localhost:5099/webhooks/nhattin/status",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var billSvc = new BillService(db, new RecordingWebhookDispatcher());
        var bill = await billSvc.CreateAsync(SampleInput(), CancellationToken.None);
        await billSvc.SetStatusAsync(bill.BillCode, 3, "Đã lấy hàng", CancellationToken.None);
        var billEntity = db.Bills.Single(b => b.BillCode == bill.BillCode);

        var handler = new StubHandler();
        var dispatcher = new HttpWebhookDispatcher(db, new StubFactory(handler));

        await dispatcher.DispatchAsync(billEntity.Id, CancellationToken.None);

        var otherSub = db.WebhookSubscriptions.Single(s => s.PartnerId == 999999);
        Assert.Contains(db.WebhookDeliveryLogs, l => l.BillCode == bill.BillCode && l.SubscriptionId == otherSub.Id);
    }
}
