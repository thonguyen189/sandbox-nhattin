using System.Net;
using System.Net.Http;
using System.Text;
using NhatTinLogistics.Sdk;
using NhatTinLogistics.Sdk.Client;
using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Tests.Infrastructure;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class PrintResultTests
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
    public async Task PrintAsync_json_error_envelope_is_unsuccessful()
    {
        var (http, _, options) = Build(
            _ => TestResponses.Ok("{\"success\":false,\"data\":[],\"message\":\"[ERR-00019]Unknow error. Please contact admin\"}"),
            partnerId: 124823);
        var api = new BillApi(http, options);

        var result = await api.PrintAsync("CP1");

        Assert.False(result.Success);
        Assert.True(result.IsJson);
        Assert.Equal("ERR-00019", result.ErrorCode);
        Assert.Contains("Unknow error", result.Message);
    }

    [Fact]
    public async Task PrintAsync_html_is_successful()
    {
        var (http, _, options) = Build(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html><body>LABEL-CP1</body></html>", Encoding.UTF8, "text/html"),
            },
            partnerId: 124823);
        var api = new BillApi(http, options);

        var result = await api.PrintAsync("CP1");

        Assert.True(result.Success);
        Assert.True(result.IsHtml);
        Assert.Contains("LABEL-CP1", result.AsText());
    }
}
