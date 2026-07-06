using System.Net.Http;
using NhatTinLogistics.Sdk;
using NhatTinLogistics.Sdk.Client;
using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Tests.Infrastructure;
using NhatTinLogistics.Sdk.Types.Requests;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class BillApiOpsTests
{
    private static (BillApi api, StubHttpMessageHandler handler) Build(
        Func<HttpRequestMessage, HttpResponseMessage> responder, int? partnerId = null)
    {
        var options = new NhatTinLogisticsClientOptions { AutoAuthenticate = false, BaseUrl = "https://test.local", PartnerId = partnerId };
        var handler = new StubHttpMessageHandler(responder);
        var http = new HttpClient(handler) { BaseAddress = new Uri(options.ResolveBaseUrl()) };
        return (new BillApi(new NhatTinHttpClient(http, options, new InMemoryTokenStore()), options), handler);
    }

    [Fact]
    public async Task CancelAsync_posts_bill_code_array_and_maps_doCode()
    {
        var (api, handler) = Build(_ =>
            TestResponses.Ok("{\"success\":true,\"data\":[{\"doCode\":\"E9\",\"message\":\"ok\"}]}"));

        var resp = await api.CancelAsync(new[] { "CP1" });

        Assert.True(resp.IsSuccess);
        Assert.Equal("E9", resp.Data![0].DoCode);
        Assert.EndsWith("/v3/bill/destroy", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Contains("\"bill_code\":[\"CP1\"]", handler.RequestBodies[0]);
    }

    [Fact]
    public async Task RevertAsync_maps_success_and_failed_lists()
    {
        var (api, handler) = Build(_ =>
            TestResponses.Ok("{\"success\":true,\"data\":{\"success\":[\"CP1\"],\"failed\":[]}}"));

        var resp = await api.RevertAsync(new[] { "CP1" });

        Assert.Single(resp.Data!.Success);
        Assert.Empty(resp.Data.Failed);
        Assert.EndsWith("/v3/bill/revert-bill", handler.Requests[0].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task CalcFeeAsync_defaults_partner_id_and_maps_options()
    {
        var (api, handler) = Build(_ =>
            TestResponses.Ok("{\"success\":true,\"data\":[{\"service_id\":21,\"service_name\":\"MES\",\"total_fee\":109910}]}"),
            partnerId: 123736);

        var resp = await api.CalcFeeAsync(new CalcFeeRequest { Weight = 1.3, PaymentMethodId = 10, SProvinceId = "79", SWardId = "27007", RProvinceId = "01", RWardId = "00004" });

        Assert.Equal(21, resp.Data![0].ServiceId);
        Assert.Equal(109910m, resp.Data[0].TotalFee);
        Assert.EndsWith("/v3/bill/calc-fee", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Contains("\"partner_id\":123736", handler.RequestBodies[0]);
    }

    [Fact]
    public async Task TrackingAsync_gets_with_query_and_tolerates_string_numbers()
    {
        var (api, handler) = Build(_ =>
            TestResponses.Ok("{\"success\":true,\"data\":[{\"bill_code\":\"CP1\",\"weight\":\"1.00\",\"total_fee\":\"20000\",\"bill_status_id\":4}]}"));

        var resp = await api.TrackingAsync("CP1");

        Assert.True(resp.IsSuccess);
        Assert.Equal("CP1", resp.Data![0].BillCode);
        Assert.Equal("20000", resp.Data[0].TotalFee); // numbers-as-strings preserved
        Assert.Equal(4, resp.Data[0].BillStatusId);
        Assert.Equal("/v3/bill/tracking", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Contains("bill_code=CP1", handler.Requests[0].RequestUri!.Query);
    }
}
