using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NhatTinMvc.Web.Data.Entities;
using NhatTinMvc.Web.Services;

namespace NhatTinMvc.Tests;

public sealed class WebhookIngestServiceTests
{
    // Payload trạng thái hợp lệ (snake_case) — "Đã lấy hàng".
    private const string ValidPayload =
        @"{""bill_no"":""NT123"",""ref_code"":""R1"",""status_id"":3,""status_name"":""Đã lấy hàng"",""status_time"":1700000000,""push_time"":1700000001,""shipping_fee"":41936,""is_partial"":0,""weight"":2.0,""length"":10,""width"":10,""height"":10}";

    private static readonly IReadOnlyDictionary<string, string> NoHeaders =
        new Dictionary<string, string>();

    private static WebhookIngestService NewService(NhatTinMvc.Web.Data.MvcDbContext db) =>
        new(db, new FakeHubContext(), NullLogger<WebhookIngestService>.Instance);

    [Fact]
    public async Task IngestAsync_ValidPayload_ParsesAndPersistsEvent()
    {
        using var db = TestDb.NewContext();
        var svc = NewService(db);

        var outcome = await svc.IngestAsync("POST", ValidPayload, NoHeaders, CancellationToken.None);

        Assert.False(outcome.Duplicate);
        Assert.True(outcome.ParsedOk);
        Assert.Equal("NT123", outcome.BillCode);
        Assert.Equal(3, outcome.StatusId);

        var evt = Assert.Single(db.BillStatusEvents.ToList());
        Assert.Equal("NT123|3|1700000000", evt.DedupeKey);
        Assert.Equal("NT123", evt.BillCode);
        Assert.Equal(3, evt.StatusId);
    }

    [Fact]
    public async Task IngestAsync_MatchingTrackedBill_UpdatesLastStatusAndLinksEvent()
    {
        using var db = TestDb.NewContext();
        db.TrackedBills.Add(new TrackedBill { BillCode = "NT123", LastStatusId = 2, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var svc = NewService(db);
        await svc.IngestAsync("POST", ValidPayload, NoHeaders, CancellationToken.None);

        var bill = await db.TrackedBills.FirstAsync(b => b.BillCode == "NT123");
        Assert.Equal(3, bill.LastStatusId);

        var evt = Assert.Single(db.BillStatusEvents.ToList());
        Assert.Equal(bill.Id, evt.TrackedBillId);
    }

    [Fact]
    public async Task IngestAsync_SamePayloadTwice_SecondIsDuplicate()
    {
        using var db = TestDb.NewContext();
        var svc = NewService(db);

        var first = await svc.IngestAsync("POST", ValidPayload, NoHeaders, CancellationToken.None);
        var second = await svc.IngestAsync("POST", ValidPayload, NoHeaders, CancellationToken.None);

        Assert.False(first.Duplicate);
        Assert.True(second.Duplicate);
        Assert.Single(db.BillStatusEvents.ToList());
    }

    [Fact]
    public async Task IngestAsync_GarbageBody_StoresRawWithoutParse()
    {
        using var db = TestDb.NewContext();
        var svc = NewService(db);

        var outcome = await svc.IngestAsync("POST", "not-json{", NoHeaders, CancellationToken.None);

        Assert.False(outcome.ParsedOk);

        var evt = Assert.Single(db.BillStatusEvents.ToList());
        Assert.Equal("not-json{", evt.RawPayload);
        Assert.Null(evt.DedupeKey);
    }
}
