using System.Text.Json;
using NhatTinLogistics.Sdk.Http;

namespace NhatTinLogistics.Sdk.Webhooks;

/// <summary>Parses raw NhatTin webhook JSON into a typed payload. Stateless — no client needed.</summary>
public static class NhatTinWebhookParser
{
    public static WebhookPayload Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new NhatTinApiException("Webhook payload is empty.");
        try
        {
            var payload = JsonSerializer.Deserialize<WebhookPayload>(json, NhatTinJson.Options);
            if (payload is null)
                throw new NhatTinApiException("Webhook payload deserialized to null.", 0, json);
            return payload;
        }
        catch (JsonException ex)
        {
            throw new NhatTinApiException("Failed to parse webhook payload as JSON.", 0, json, ex);
        }
    }

    public static bool TryParse(string json, out WebhookPayload payload)
    {
        try
        {
            payload = Parse(json);
            return true;
        }
        catch (NhatTinApiException)
        {
            payload = new WebhookPayload();
            return false;
        }
    }
}
