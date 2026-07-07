using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NhatTinLogistics.Sdk.Http;

/// <summary>Shared JSON options for all NhatTin serialization. snake_case comes from explicit [JsonPropertyName].</summary>
public static class NhatTinJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        // NhatTin sends numbers/nulls where strings are expected (verified live 2026-07-07);
        // tolerate that globally so string properties never throw on a raw number.
        Converters = { new TolerantStringConverter() },
    };
}
