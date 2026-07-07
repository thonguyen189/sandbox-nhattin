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
    public async Task CancelAsync_maps_success_and_failed_object()
    {
        // Real sandbox shape: data is an OBJECT { success:[{doCode,message}], failed:[] },
        // NOT a bare array. Captured live 2026-07-07 (CP252694164).
        var (api, handler) = Build(_ =>
            TestResponses.Ok("{\"success\":true,\"data\":{\"success\":[{\"doCode\":\"CP1\",\"message\":\"Bill CP1 has canceled successful.\"}],\"failed\":[]},\"message\":\"Bill canceled successfully\"}"));

        var resp = await api.CancelAsync(new[] { "CP1" });

        Assert.True(resp.IsSuccess);
        Assert.Single(resp.Data!.Succeeded);
        Assert.Equal("CP1", resp.Data.Succeeded[0].DoCode);
        Assert.EndsWith("has canceled successful.", resp.Data.Succeeded[0].Message);
        Assert.Empty(resp.Data.Failed);
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
    public async Task CalcFeeAsync_tolerates_null_service_id()
    {
        // Real sandbox returns service_id:null on calc-fee. Captured live 2026-07-07.
        var (api, _) = Build(_ =>
            TestResponses.Ok("{\"success\":true,\"data\":[{\"weight\":2,\"total_fee\":41936,\"main_fee\":41936,\"service_id\":null,\"lead_time\":\"2026-01-01 17:55:00\"}],\"message\":\"Calculate Successfull\"}"),
            partnerId: 124823);

        var resp = await api.CalcFeeAsync(new CalcFeeRequest { Weight = 2, PaymentMethodId = 10, SProvinceId = "01", SWardId = "00004", RProvinceId = "79", RWardId = "25750" });

        Assert.True(resp.IsSuccess);
        Assert.Null(resp.Data![0].ServiceId);
        Assert.Equal(41936m, resp.Data[0].TotalFee);
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

    [Fact]
    public async Task TrackingAsync_tolerates_raw_numbers_and_null_fee_fields()
    {
        // Real sandbox is inconsistent: measures arrive as strings ("weight":"2") but fees
        // arrive as raw JSON numbers ("cod_amt":0, "main_fee":41936) and some are null
        // ("lifting_fee":null). Captured live 2026-07-07 (CP252694164).
        var (api, _) = Build(_ =>
            TestResponses.Ok("{\"success\":true,\"data\":[{\"bill_code\":\"CP1\",\"weight\":\"2\",\"cod_amt\":0,\"main_fee\":41936,\"total_fee\":41936,\"lifting_fee\":null,\"bill_status_id\":2}]}"));

        var resp = await api.TrackingAsync("CP1");

        Assert.True(resp.IsSuccess);
        Assert.Equal("2", resp.Data![0].Weight);       // JSON string preserved
        Assert.Equal("0", resp.Data[0].CodAmount);     // JSON number → string
        Assert.Equal("41936", resp.Data[0].MainFee);   // JSON number → string
        Assert.Equal("41936", resp.Data[0].TotalFee);
        Assert.Null(resp.Data[0].LiftingFee);          // JSON null preserved
        Assert.Equal(2, resp.Data[0].BillStatusId);
    }
}
