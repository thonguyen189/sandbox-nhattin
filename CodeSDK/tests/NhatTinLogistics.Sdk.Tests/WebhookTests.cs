using NhatTinLogistics.Sdk.Types.Enums;
using NhatTinLogistics.Sdk.Webhooks;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class WebhookTests
{
    private const string Sample =
        "{\"weight\":2,\"bill_no\":\"CP16658276R\",\"status_time\":1681382601,\"shipping_fee\":38610," +
        "\"is_partial\":1,\"status_name\":\"Đã lấy hàng\",\"status_id\":3,\"dimension_weight\":1," +
        "\"length\":1,\"width\":1,\"height\":1,\"push_time\":1681382738,\"ref_code\":\"40724974\"," +
        "\"expected_at\":\"2024-08-02 09:00:00\"}";

    [Fact]
    public void Parse_maps_fields_and_status_enum()
    {
        var p = NhatTinWebhookParser.Parse(Sample);

        Assert.Equal("CP16658276R", p.BillNo);
        Assert.Equal(3, p.StatusId);
        Assert.Equal(BillStatus.PickedUp, p.Status);
        Assert.Equal(38610m, p.ShippingFee);
        Assert.True(p.IsPartialReturn);
        Assert.Equal(1681382601, p.StatusTime);
        Assert.Equal(2024, p.ExpectedAtUtc!.Value.Year);
    }

    [Fact]
    public void TryParse_returns_false_on_malformed_json()
    {
        Assert.False(NhatTinWebhookParser.TryParse("{not json", out _));
    }

    [Fact]
    public void TryParse_returns_true_on_valid()
    {
        Assert.True(NhatTinWebhookParser.TryParse(Sample, out var p));
        Assert.Equal("40724974", p.RefCode);
    }
}
