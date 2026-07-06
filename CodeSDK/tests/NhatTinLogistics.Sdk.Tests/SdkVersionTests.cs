using NhatTinLogistics.Sdk;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class SdkVersionTests
{
    [Fact]
    public void Current_is_semver_like()
    {
        Assert.Matches(@"^\d+\.\d+\.\d+$", SdkVersion.Current);
    }
}
