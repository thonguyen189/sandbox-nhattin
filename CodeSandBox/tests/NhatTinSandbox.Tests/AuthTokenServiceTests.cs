using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NhatTinSandbox.Infrastructure.Auth;
using NhatTinSandbox.Infrastructure.Persistence;
using Xunit;

namespace NhatTinSandbox.Tests;

public sealed class AuthTokenServiceTests
{
    private static SandboxDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<SandboxDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new SandboxDbContext(options);
        db.Database.EnsureCreated();
        SeedData.EnsureSeeded(db);
        return db;
    }

    private static JwtAuthTokenService NewService(SandboxDbContext db)
    {
        var opt = Options.Create(new JwtOptions
        {
            Issuer = "nhattin-sandbox",
            Audience = "nhattin-sandbox",
            SigningKey = "sandbox-signing-key-please-change-0123456789",
            AccessTtlSeconds = 86400,   // 24h
            RefreshTtlSeconds = 604800  // 7d
        });
        return new JwtAuthTokenService(db, opt);
    }

    [Fact]
    public async Task SignIn_WithSeededDemoAccount_ReturnsBearerTokenPair()
    {
        using var db = NewDb();
        var svc = NewService(db);

        var result = await svc.SignInAsync("sandbox", "sandbox123", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Bearer", result!.TokenType);
        Assert.False(string.IsNullOrWhiteSpace(result.JwtToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.Equal(86400, result.TokenExpiresInSeconds);
        Assert.True(result.RefreshExpiresInSeconds > result.TokenExpiresInSeconds);
    }

    [Fact]
    public async Task SignIn_WithWrongPassword_ReturnsNull()
    {
        using var db = NewDb();
        var svc = NewService(db);

        var result = await svc.SignInAsync("sandbox", "wrong", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Refresh_WithIssuedToken_ReturnsNewPair()
    {
        using var db = NewDb();
        var svc = NewService(db);
        var first = await svc.SignInAsync("sandbox", "sandbox123", CancellationToken.None);

        var refreshed = await svc.RefreshAsync(first!.RefreshToken, CancellationToken.None);

        Assert.NotNull(refreshed);
        Assert.False(string.IsNullOrWhiteSpace(refreshed!.JwtToken));
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_ReturnsNull()
    {
        using var db = NewDb();
        var svc = NewService(db);

        var refreshed = await svc.RefreshAsync("not-a-real-token", CancellationToken.None);

        Assert.Null(refreshed);
    }

    [Fact]
    public async Task Refresh_WithDeactivatedAccount_ReturnsNull()
    {
        using var db = NewDb();
        var svc = NewService(db);
        var first = await svc.SignInAsync("sandbox", "sandbox123", CancellationToken.None);

        var account = db.PartnerAccounts.Single(a => a.Username == "sandbox");
        account.IsActive = false;
        db.SaveChanges();

        var refreshed = await svc.RefreshAsync(first!.RefreshToken, CancellationToken.None);

        Assert.Null(refreshed);
    }
}
