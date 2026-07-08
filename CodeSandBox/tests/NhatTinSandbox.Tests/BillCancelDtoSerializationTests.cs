using System.Text.Json;
using System.Text.Json.Serialization;
using NhatTinSandbox.Api.Json;
using Xunit;

namespace NhatTinSandbox.Tests;

/// <summary>
/// Mirrors NhatTinSandbox.Api.Controllers.BillController.CancelResultDto.
/// The Api applies a global JsonNamingPolicy.SnakeCaseLower policy (Program.cs) which would
/// otherwise rewrite "DoCode" -> "do_code". The documented contract for /v3/bill/destroy
/// (NhatTinAPIDocumentation/vi/bill/cancelbill.md) requires the camelCase "doCode" field
/// verbatim, so it must be pinned via [JsonPropertyName] and preserved regardless of the
/// global policy.
/// </summary>
public sealed record CancelResultDto(
    [property: JsonPropertyName("doCode")] string DoCode,
    string Message);

public sealed class BillCancelDtoSerializationTests
{
    private static readonly JsonSerializerOptions ApiJsonOptions = new()
    {
        PropertyNamingPolicy = SnakeCaseLowerNamingPolicy.Instance
    };

    [Fact]
    public void CancelResultDto_SerializesDoCode_NotSnakeCase_UnderGlobalSnakeCasePolicy()
    {
        var dto = new CancelResultDto("E9999999", "Bill E9999999 has canceled successful");

        var json = JsonSerializer.Serialize(dto, ApiJsonOptions);

        Assert.Contains("\"doCode\"", json);
        Assert.DoesNotContain("do_code", json);
        // Message has no explicit override, so the global policy still applies to it.
        Assert.Contains("\"message\"", json);
    }
}
