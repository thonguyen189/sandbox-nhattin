using System.Net.Http;

namespace NhatTinLogistics.Sdk.Tests.Infrastructure;

/// <summary>Deterministic HttpMessageHandler for tests. Captures requests + bodies, returns a scripted response.</summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
    private readonly object _gate = new();

    public List<HttpRequestMessage> Requests { get; } = new();
    public List<string> RequestBodies { get; } = new();
    public int CallCount { get { lock (_gate) { return Requests.Count; } } }

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        => _responder = responder;

    // Thread-safe: the concurrency test fires parallel requests through one handler.
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
        lock (_gate)
        {
            Requests.Add(request);
            RequestBodies.Add(body);
        }
        return _responder(request);
    }
}
