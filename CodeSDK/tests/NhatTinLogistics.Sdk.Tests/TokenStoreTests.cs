using NhatTinLogistics.Sdk.Http;
using Xunit;

namespace NhatTinLogistics.Sdk.Tests;

public class TokenStoreTests
{
    [Fact]
    public void SetTokens_then_read_back()
    {
        ITokenStore store = new InMemoryTokenStore();
        Assert.Null(store.AccessToken);

        store.SetTokens("acc", "ref");
        Assert.Equal("acc", store.AccessToken);
        Assert.Equal("ref", store.RefreshToken);

        store.Clear();
        Assert.Null(store.AccessToken);
        Assert.Null(store.RefreshToken);
    }
}
