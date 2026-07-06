using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NhatTinSandbox.Application.Auth;
using NhatTinSandbox.Domain.Entities;
using NhatTinSandbox.Infrastructure.Persistence;

namespace NhatTinSandbox.Infrastructure.Auth;

public sealed class JwtAuthTokenService : IAuthTokenService
{
    private readonly SandboxDbContext _db;
    private readonly JwtOptions _opt;

    public JwtAuthTokenService(SandboxDbContext db, IOptions<JwtOptions> opt)
    {
        _db = db;
        _opt = opt.Value;
    }

    public async Task<AuthTokenResult?> SignInAsync(string username, string password, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        var hash = SeedData.Hash(password);
        var account = await _db.PartnerAccounts
            .FirstOrDefaultAsync(a => a.Username == username && a.IsActive, ct);

        if (account is null || account.PasswordHash != hash)
            return null;

        return await IssueAsync(account, ct);
    }

    public async Task<AuthTokenResult?> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var tokenHash = SeedData.Hash(refreshToken);
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && !t.IsRevoked, ct);

        if (stored is null || stored.ExpiresAt <= DateTimeOffset.UtcNow)
            return null;

        stored.IsRevoked = true; // rotate
        var account = await _db.PartnerAccounts
            .FirstOrDefaultAsync(a => a.Id == stored.AccountId && a.IsActive, ct);
        if (account is null) return null; // deactivated account cannot rotate refresh tokens
        return await IssueAsync(account, ct);
    }

    private async Task<AuthTokenResult> IssueAsync(PartnerAccount account, CancellationToken ct)
    {
        var jwt = BuildJwt(account);
        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

        _db.RefreshTokens.Add(new RefreshToken
        {
            AccountId = account.Id,
            TokenHash = SeedData.Hash(refresh),
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(_opt.RefreshTtlSeconds),
            IsRevoked = false
        });
        await _db.SaveChangesAsync(ct);

        return new AuthTokenResult(
            JwtToken: jwt,
            TokenType: "Bearer",
            TokenExpiresInSeconds: _opt.AccessTtlSeconds,
            RefreshToken: refresh,
            RefreshExpiresInSeconds: _opt.RefreshTtlSeconds);
    }

    private string BuildJwt(PartnerAccount account)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new Claim("partner_id", account.PartnerId.ToString()),
            new Claim("username", account.Username)
        };
        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(_opt.AccessTtlSeconds),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
