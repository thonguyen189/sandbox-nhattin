using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NhatTinLogistics.Sdk.Http;

/// <summary>
/// Reads a JSON <c>string</c> property from a value that the NhatTin API may send as a string,
/// a raw number, a boolean, or null. The sandbox is inconsistent — e.g. tracking returns
/// <c>"weight":"2"</c> (string) but <c>"cod_amt":0</c> and <c>"main_fee":41936</c> (raw numbers),
/// and <c>"lifting_fee":null</c> — so a plain <c>string?</c> property would throw. Numbers are
/// preserved as their exact source text; null stays null. Registered globally in
/// <see cref="NhatTinJson"/> so every string property tolerates this without per-field attributes.
/// </summary>
public sealed class TolerantStringConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.Number:
                // Preserve the exact numeric text (e.g. "41936", "1.00") without reformatting.
                return reader.HasValueSequence
                    ? Encoding.UTF8.GetString(reader.ValueSequence.ToArray())
                    : Encoding.UTF8.GetString(reader.ValueSpan);
            case JsonTokenType.True:
                return "true";
            case JsonTokenType.False:
                return "false";
            default:
                // Objects/arrays where a string was expected: keep the raw JSON rather than throw.
                using (var doc = JsonDocument.ParseValue(ref reader))
                    return doc.RootElement.GetRawText();
        }
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteStringValue(value);
    }
}
