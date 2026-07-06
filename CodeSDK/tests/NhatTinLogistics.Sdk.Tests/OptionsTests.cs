using NhatTinLogistics.Sdk;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class OptionsTests
{
    [Fact]
    public void ResolveBaseUrl_uses_environment_host()
    {
        Assert.Equal("https://apisandbox.ntlogistics.vn",
            new NhatTinLogisticsClientOptions { Environment = NhatTinEnvironment.Sandbox }.ResolveBaseUrl());
        Assert.Equal("https://apiws.ntlogistics.vn",
            new NhatTinLogisticsClientOptions { Environment = NhatTinEnvironment.Production }.ResolveBaseUrl());
    }

    [Fact]
    public void ResolveBaseUrl_prefers_explicit_baseurl_trimmed()
    {
        var o = new NhatTinLogisticsClientOptions { BaseUrl = "https://local.test/" };
        Assert.Equal("https://local.test", o.ResolveBaseUrl());
    }

    [Fact]
    public void Validate_throws_when_autoauth_without_credentials()
    {
        var o = new NhatTinLogisticsClientOptions { AutoAuthenticate = true };
        Assert.Throws<ArgumentException>(() => o.Validate());
    }

    [Fact]
    public void Validate_ok_when_autoauth_disabled()
    {
        var o = new NhatTinLogisticsClientOptions { AutoAuthenticate = false };
        o.Validate(); // must not throw
    }
}
