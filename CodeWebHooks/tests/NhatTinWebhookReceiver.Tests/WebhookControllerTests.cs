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

    [Fact]
    public async Task Duplicate_webhook_is_stored_once()
    {
        var client = _factory.CreateClient();
        const string body =
            "{\"bill_no\":\"CP1\",\"status_id\":3,\"status_name\":\"x\",\"status_time\":1730000000,\"push_time\":1730000005}";

        var content1 = new StringContent(body, Encoding.UTF8, "application/json");
        var resp1 = await client.PostAsync("/webhooks/nhattin/status", content1);
        Assert.Equal(HttpStatusCode.OK, resp1.StatusCode);

        var content2 = new StringContent(body, Encoding.UTF8, "application/json");
        var resp2 = await client.PostAsync("/webhooks/nhattin/status", content2);
        Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WebhookDbContext>();
        var count = db.ReceivedWebhooks.Count(w =>
            w.BillNo == "CP1" && w.StatusId == 3 && w.StatusTime == 1730000000L);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Parses_status_time_and_push_time()
    {
        var client = _factory.CreateClient();
        const string body =
            "{\"bill_no\":\"CP-TIME\",\"status_id\":7,\"status_name\":\"x\",\"status_time\":1730000000,\"push_time\":1730000005}";
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var resp = await client.PostAsync("/webhooks/nhattin/status", content);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WebhookDbContext>();
        var row = db.ReceivedWebhooks.OrderByDescending(w => w.Id).First(w => w.BillNo == "CP-TIME");
        Assert.Equal(1730000000L, row.StatusTime);
        Assert.Equal(1730000005L, row.PushTime);
    }
}
