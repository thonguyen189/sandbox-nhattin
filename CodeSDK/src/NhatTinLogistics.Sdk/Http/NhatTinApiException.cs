namespace NhatTinLogistics.Sdk.Http;

/// <summary>Thrown for transport, JSON-parse, or authentication failures. Business failures (success:false) do NOT throw.</summary>
public sealed class NhatTinApiException : Exception
{
    public int HttpStatusCode { get; }
    public string? RawBody { get; }

    public NhatTinApiException(string message, int httpStatusCode = 0, string? rawBody = null, Exception? inner = null)
        : base(message, inner)
    {
        HttpStatusCode = httpStatusCode;
        RawBody = rawBody;
    }
}
