using System.Text.Json;
using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Types.Enums;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class FoundationTests
{
    [Fact]
    public void EnsureSuccess_throws_when_not_successful()
    {
        var resp = new NhatTinResponse<string> { Success = false, Message = "bad", HttpStatusCode = 200, RawBody = "{}" };
        var ex = Assert.Throws<NhatTinApiException>(() => resp.EnsureSuccess());
        Assert.Contains("bad", ex.Message);
    }

    [Fact]
    public void EnsureSuccess_returns_self_when_successful()
    {
        var resp = new NhatTinResponse<string> { Success = true, Data = "ok" };
        Assert.Same(resp, resp.EnsureSuccess());
        Assert.True(resp.IsSuccess);
    }

    [Fact]
    public void RawEnvelope_deserializes_success_message_data()
    {
        var env = JsonSerializer.Deserialize<RawEnvelope<int>>(
            "{\"success\":true,\"message\":\"m\",\"data\":42}", NhatTinJson.Options);
        Assert.NotNull(env);
        Assert.True(env!.Success);
        Assert.Equal("m", env.Message);
        Assert.Equal(42, env.Data);
    }

    [Theory]
    [InlineData(4, BillStatus.Delivered)]
    [InlineData(2, BillStatus.WaitingPickup)]
    [InlineData(99, BillStatus.Unknown)]
    public void ToBillStatus_maps_known_and_unknown(int id, BillStatus expected)
        => Assert.Equal(expected, id.ToBillStatus());
}
