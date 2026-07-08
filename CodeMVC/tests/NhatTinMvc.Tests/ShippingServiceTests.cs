using Microsoft.Extensions.Logging.Abstractions;
using NhatTinLogistics.Sdk;
using NhatTinMvc.Web.Data.Entities;
using NhatTinMvc.Web.Services;

namespace NhatTinMvc.Tests;

public sealed class ShippingServiceTests
{
    // Client offline: AutoAuthenticate=false → không auto sign-in; BaseUrl loopback không dùng vì các test này không gọi mạng.
    private static (ShippingService Svc, NhatTinLogisticsClient Client) NewService(NhatTinMvc.Web.Data.MvcDbContext db)
    {
        var options = new NhatTinLogisticsClientOptions { AutoAuthenticate = false, BaseUrl = "http://localhost:9" };
        var client = new NhatTinLogisticsClient(options);
        var svc = new ShippingService(client, options, db, new DemoAuthState(), NullLogger<ShippingService>.Instance);
        return (svc, client);
    }

    [Fact]
    public void GetAuthStatus_NotSignedIn_ReportsUnauthenticated()
    {
        using var db = TestDb.NewContext();
        var (svc, client) = NewService(db);
        using (client)
        {
            var status = svc.GetAuthStatus();

            Assert.False(status.IsAuthenticated);
            Assert.Null(status.PartnerId);
        }
    }

    [Fact]
    public async Task GetTrackedBillsAsync_ReturnsBillsOrderedByCreatedAtDescending()
    {
        using var db = TestDb.NewContext();
        var older = new TrackedBill { BillCode = "NT-OLD", CreatedAt = DateTimeOffset.UtcNow.AddHours(-2) };
        var newer = new TrackedBill { BillCode = "NT-NEW", CreatedAt = DateTimeOffset.UtcNow.AddHours(-1) };
        db.TrackedBills.AddRange(older, newer);
        await db.SaveChangesAsync();

        var (svc, client) = NewService(db);
        using (client)
        {
            var bills = await svc.GetTrackedBillsAsync();

            Assert.Equal(2, bills.Count);
            Assert.Equal("NT-NEW", bills[0].BillCode);
            Assert.Equal("NT-OLD", bills[1].BillCode);
        }
    }
}
