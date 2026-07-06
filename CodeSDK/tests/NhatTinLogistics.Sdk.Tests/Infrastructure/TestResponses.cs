using System.Net;
using System.Net.Http;
using System.Text;

namespace NhatTinLogistics.Sdk.Tests.Infrastructure;

public static class TestResponses
{
    public static HttpResponseMessage Json(HttpStatusCode code, string body)
        => new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    public static HttpResponseMessage Ok(string body) => Json(HttpStatusCode.OK, body);
}
