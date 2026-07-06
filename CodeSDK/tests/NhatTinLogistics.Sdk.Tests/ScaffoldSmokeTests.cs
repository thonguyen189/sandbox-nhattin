using System.Net;
using System.Net.Http;
using NhatTinLogistics.Sdk.Tests.Infrastructure;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class ScaffoldSmokeTests
{
    [Fact]
    public async Task Stub_handler_returns_scripted_response_and_records_request()
    {
        var handler = new StubHttpMessageHandler(_ => TestResponses.Ok("{\"success\":true}"));
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test") };

        var resp = await http.GetAsync("/ping");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Single(handler.Requests);
    }
}
