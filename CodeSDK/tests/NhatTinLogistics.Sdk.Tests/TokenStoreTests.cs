using System;
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

    [Fact]
    public void SetTokens_without_expiry_leaves_expiry_null()
    {
        ITokenStore store = new InMemoryTokenStore();
        store.SetTokens("acc", "ref");
        Assert.Null(store.AccessTokenExpiresAt);
        Assert.Null(store.RefreshTokenExpiresAt);
    }

    [Fact]
    public void SetTokens_with_expiry_reads_back_and_clear_wipes_it()
    {
        var accessExp = new DateTimeOffset(2026, 7, 8, 12, 0, 0, TimeSpan.Zero);
        var refreshExp = new DateTimeOffset(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);
        ITokenStore store = new InMemoryTokenStore();

        store.SetTokens("acc", "ref", accessExp, refreshExp);

        Assert.Equal(accessExp, store.AccessTokenExpiresAt);
        Assert.Equal(refreshExp, store.RefreshTokenExpiresAt);

        store.Clear();
        Assert.Null(store.AccessTokenExpiresAt);
        Assert.Null(store.RefreshTokenExpiresAt);
    }
}
