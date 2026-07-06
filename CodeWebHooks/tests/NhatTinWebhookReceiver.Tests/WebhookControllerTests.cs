using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace NhatTinWebhookReceiver.Tests;

public sealed class WebhookControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public WebhookControllerTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task PostStatus_ReturnsAck()
    {
        var client = _factory.CreateClient();
        var payload = new
        {
            bill_no = "CP16658276R",
            ref_code = "40724974",
            status_id = 3,
            status_name = "Đã lấy hàng",
            status_time = 1681382601,
            push_time = 1681382738
        };

        var resp = await client.PostAsJsonAsync("/webhooks/nhattin/status", payload);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        Assert.Contains("ACK", json);
    }
}
