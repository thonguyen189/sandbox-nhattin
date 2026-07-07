using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NhatTinWebhookReceiver.Api.Persistence;
using Xunit;

namespace NhatTinWebhookReceiver.Tests;

public sealed class WebhookControllerTests : IClassFixture<WebhookApiFactory>
{
    private readonly WebhookApiFactory _factory;
    public WebhookControllerTests(WebhookApiFactory factory) => _factory = factory;

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

    [Fact]
    public async Task PostStatus_WithJsonArrayRoot_StillAcksAndStoresRawEvidence()
    {
        var client = _factory.CreateClient();
        var content = new StringContent("[1,2,3]", Encoding.UTF8, "application/json");

        var resp = await client.PostAsync("/webhooks/nhattin/status", content);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        Assert.Contains("ACK", json);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WebhookDbContext>();
        var row = db.ReceivedWebhooks.OrderByDescending(w => w.Id).First(w => w.RawBody == "[1,2,3]");
        Assert.False(row.IsValidPayload);
        Assert.False(string.IsNullOrEmpty(row.RawBody));
    }

    [Fact]
    public async Task PostStatus_WithWrongTypedField_StillAcksAndStoresRawEvidence()
    {
        var client = _factory.CreateClient();
        const string body = "{\"bill_no\": 123, \"status_id\": 3}";
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var resp = await client.PostAsync("/webhooks/nhattin/status", content);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        Assert.Contains("ACK", json);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WebhookDbContext>();
        var row = db.ReceivedWebhooks.OrderByDescending(w => w.Id).First(w => w.RawBody == body);
        Assert.False(row.IsValidPayload);
        Assert.False(string.IsNullOrEmpty(row.RawBody));
    }
}
