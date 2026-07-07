using System;
using NhatTinLogistics.Sdk.Http;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class TokenTtlTests
{
    [Theory]
    [InlineData("24h", 24 * 3600)]
    [InlineData("7d", 7 * 86400)]
    [InlineData("3600s", 3600)]
    [InlineData("900s", 900)]
    [InlineData("30m", 30 * 60)]
    [InlineData("120", 120)]        // bare number is interpreted as seconds
    [InlineData(" 24h ", 24 * 3600)] // surrounding whitespace tolerated
    public void Parse_valid_ttl_returns_timespan(string ttl, int expectedSeconds)
    {
        var result = TokenTtl.Parse(ttl);
        Assert.NotNull(result);
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), result!.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("12x")]
    [InlineData("h")]
    public void Parse_invalid_ttl_returns_null(string? ttl)
    {
        Assert.Null(TokenTtl.Parse(ttl));
    }
}
