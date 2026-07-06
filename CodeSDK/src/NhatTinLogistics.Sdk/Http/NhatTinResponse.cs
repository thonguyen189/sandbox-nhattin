using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

[assembly: InternalsVisibleTo("NhatTinLogistics.Sdk.Tests")]

namespace NhatTinLogistics.Sdk.Http;

/// <summary>Public result wrapper mapped from the NhatTin { success, message, data } envelope.</summary>
public sealed class NhatTinResponse<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }
    public int HttpStatusCode { get; init; }
    public string RawBody { get; init; } = "";

    public bool IsSuccess => Success;

    public NhatTinResponse<T> EnsureSuccess()
    {
        if (!Success)
            throw new NhatTinApiException($"NhatTin API returned failure: {Message}", HttpStatusCode, RawBody);
        return this;
    }
}

/// <summary>Wire shape of the envelope. Internal — consumers use NhatTinResponse&lt;T&gt;.</summary>
internal sealed class RawEnvelope<T>
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("data")] public T? Data { get; set; }
}
