using System.Net.Http;
using NhatTinLogistics.Sdk;
using NhatTinLogistics.Sdk.Client;
using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Tests.Infrastructure;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class PrintAndLocationTests
{
    private static (NhatTinHttpClient http, StubHttpMessageHandler handler, NhatTinLogisticsClientOptions options) Build(
        Func<HttpRequestMessage, HttpResponseMessage> responder, int? partnerId = null)
    {
        var options = new NhatTinLogisticsClientOptions { AutoAuthenticate = false, BaseUrl = "https://test.local", PartnerId = partnerId };
        var handler = new StubHttpMessageHandler(responder);
        var http = new HttpClient(handler) { BaseAddress = new Uri(options.ResolveBaseUrl()) };
        return (new NhatTinHttpClient(http, options, new InMemoryTokenStore()), handler, options);
    }

    [Fact]
    public void GetPrintUrl_builds_expected_url()
    {
        var (http, _, options) = Build(_ => TestResponses.Ok("{}"), partnerId: 123736);
        var api = new BillApi(http, options);

        var url = api.GetPrintUrl("CP1");

        Assert.Equal("https://test.local/v3/bill/print?do_code=CP1&partner_id=123736", url);
    }

    [Fact]
    public void GetPrintUrl_throws_when_no_partner_id()
    {
        var (http, _, options) = Build(_ => TestResponses.Ok("{}"));
        var api = new BillApi(http, options);
        Assert.Throws<ArgumentException>(() => api.GetPrintUrl("CP1"));
    }

    [Fact]
    public async Task GetProvincesAsync_sends_is_new_and_maps()
    {
        var (http, handler, _) = Build(_ =>
            TestResponses.Ok("{\"success\":true,\"data\":[{\"id\":\"11\",\"province_name\":\"Cao Bằng\",\"is_new\":\"N\"}]}"));
        var api = new LocationApi(http);

        var resp = await api.GetProvincesAsync(isNew: true);

        Assert.Equal("11", resp.Data![0].Id);
        Assert.Equal("Cao Bằng", resp.Data[0].ProvinceName);
        Assert.Equal("/v3/loc/provinces", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Contains("is_new=1", handler.Requests[0].RequestUri!.Query);
    }

    [Fact]
    public async Task GetWardsAsync_includes_optional_params()
    {
        var (http, handler, _) = Build(_ =>
            TestResponses.Ok("{\"success\":true,\"data\":[{\"id\":\"01106\",\"ward_name\":\"P.Hồng Gai\",\"is_new\":\"N\"}]}"));
        var api = new LocationApi(http);

        await api.GetWardsAsync(districtId: null, provinceId: "01", isNew: true);

        var q = handler.Requests[0].RequestUri!.Query;
        Assert.Contains("is_new=1", q);
        Assert.Contains("province_id=01", q);
        Assert.DoesNotContain("district_id", q);
    }
}
