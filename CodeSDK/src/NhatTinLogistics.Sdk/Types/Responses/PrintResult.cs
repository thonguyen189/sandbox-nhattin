using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using NhatTinLogistics.Sdk.Http;

namespace NhatTinLogistics.Sdk.Types.Responses;

/// <summary>
/// Content-type-aware result of a bill print request. NhatTin returns HTTP 200 for both a successful
/// print (HTML label) and a business error (a JSON <c>{ success:false, message }</c> envelope), so the
/// HTTP status alone is not a reliable success signal — inspect <see cref="Success"/>.
/// </summary>
public sealed class PrintResult
{
    private static readonly Regex ErrorCodeRegex =
        new(@"\[(ERR-[A-Za-z0-9_-]+)\]", RegexOptions.Compiled);

    public int HttpStatusCode { get; }
    public string? ContentType { get; }
    /// <summary>Raw response body bytes. Never null (empty array when there is no body).</summary>
    public byte[] Content { get; }

    /// <summary>Business success. For a JSON envelope this is the parsed <c>success</c> flag; otherwise a 2xx status.</summary>
    public bool Success { get; }
    /// <summary>Envelope <c>message</c> when the response is a JSON envelope; otherwise null.</summary>
    public string? Message { get; }
    /// <summary>NhatTin error code (e.g. "ERR-00019") extracted from <see cref="Message"/>, or null.</summary>
    public string? ErrorCode { get; }

    public bool IsJson => ContentType is not null
        && ContentType.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    public bool IsHtml => ContentType is not null
        && ContentType.Contains("text/html", StringComparison.OrdinalIgnoreCase);

    public PrintResult(int httpStatusCode, string? contentType, byte[]? content)
    {
        HttpStatusCode = httpStatusCode;
        ContentType = contentType;
        Content = content ?? Array.Empty<byte>();

        if (IsJson)
        {
            var (success, message) = ParseEnvelope(AsText());
            Success = success;
            Message = message;
        }
        else
        {
            Success = httpStatusCode is >= 200 and <= 299;
            Message = null;
        }

        ErrorCode = Message is not null && ErrorCodeRegex.Match(Message) is { Success: true } m
            ? m.Groups[1].Value
            : null;
    }

    /// <summary>UTF-8 decode of <see cref="Content"/>.</summary>
    public string AsText() => Encoding.UTF8.GetString(Content);

    private static (bool success, string? message) ParseEnvelope(string json)
    {
        try
        {
            var env = JsonSerializer.Deserialize<RawEnvelope<object>>(json, NhatTinJson.Options);
            return env is null ? (false, null) : (env.Success, env.Message);
        }
        catch (JsonException)
        {
            return (false, null);
        }
    }
}
