using System.Net.Http;
using NhatTinLogistics.Sdk;
using NhatTinLogistics.Sdk.Client;
using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Tests.Infrastructure;
using NhatTinLogistics.Sdk.Types.Enums;
using NhatTinLogistics.Sdk.Types.Requests;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class BillApiCreateUpdateTests
{
    private static (BillApi api, StubHttpMessageHandler handler) Build(
        Func<HttpRequestMessage, HttpResponseMessage> responder, int? partnerId = null)
    {
        var options = new NhatTinLogisticsClientOptions
        {
            AutoAuthenticate = false, BaseUrl = "https://test.local", PartnerId = partnerId
        };
        var handler = new StubHttpMessageHandler(responder);
        var http = new HttpClient(handler) { BaseAddress = new Uri(options.ResolveBaseUrl()) };
        return (new BillApi(new NhatTinHttpClient(http, options, new InMemoryTokenStore()), options), handler);
    }

    [Fact]
    public async Task CreateAsync_serializes_snake_case_and_maps_result()
    {
        var (api, handler) = Build(_ =>
            TestResponses.Ok("{\"success\":true,\"message\":\"Create bill successfully\",\"data\":{\"bill_code\":\"CP1\",\"status_id\":2}}"));

        var resp = await api.CreateAsync(new CreateBillRequest
        {
            Weight = 2, ServiceId = 91, PaymentMethodId = 10, CargoTypeId = 2,
            SName = "S", SPhone = "0333", SAddress = "a", SProvinceCode = "01", SWardCode = "00004",
            RName = "R", RPhone = "0333", RAddress = "b", RProvinceCode = "79", RWardCode = "25750",
        });

        Assert.True(resp.IsSuccess);
        Assert.Equal("CP1", resp.Data!.BillCode);
        Assert.Equal(BillStatus.WaitingPickup, resp.Data.Status);

        var body = handler.RequestBodies[0];
        Assert.EndsWith("/v3/bill/create", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Contains("\"s_province_code\":\"01\"", body);
        Assert.Contains("\"payment_method_id\":10", body);
        Assert.DoesNotContain("ref_code", body); // null fields omitted
    }

    [Fact]
    public async Task UpdateAsync_defaults_partner_id_from_options_and_targets_update_shipping()
    {
        var (api, handler) = Build(_ =>
            TestResponses.Ok("{\"success\":true,\"data\":{\"bill_code\":\"CP1\"}}"), partnerId: 123736);

        await api.UpdateAsync(new UpdateBillRequest { BillCode = "CP1", CodAmount = 200000 });

        Assert.EndsWith("/v3/bill/update-shipping", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Contains("\"partner_id\":123736", handler.RequestBodies[0]);
        Assert.Contains("\"bill_code\":\"CP1\"", handler.RequestBodies[0]);
    }
}
