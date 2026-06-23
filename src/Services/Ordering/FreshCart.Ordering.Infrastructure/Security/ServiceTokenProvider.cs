using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FreshCart.BuildingBlocks.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FreshCart.Ordering.Infrastructure.Security;

/// <summary>
/// Mints the service-to-service JWT the saga presents to Payment and Inventory. The token is signed with
/// the same symmetric key, issuer and audience the downstream services already validate, and carries the
/// <see cref="ServiceAuthenticationDefaults.ServiceAccountRole"/> role their <c>ServiceCaller</c> policy
/// requires. Minting is cheap but not free, so the token is cached and re-minted only once it is within
/// <see cref="RenewBefore"/> of expiry; a lock keeps concurrent saga steps from minting in parallel.
/// </summary>
public sealed class ServiceTokenProvider : IServiceTokenProvider
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RenewBefore = TimeSpan.FromMinutes(1);

    private readonly string issuer;
    private readonly string audience;
    private readonly SigningCredentials signingCredentials;
    private readonly TimeProvider timeProvider;
    private readonly JwtSecurityTokenHandler tokenHandler = new();
    private readonly Lock renewalLock = new();

    private string? cachedToken;
    private DateTimeOffset cachedTokenExpiresOnUtc;

    public ServiceTokenProvider(IConfiguration configuration, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer missing.");
        audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience missing.");
        var signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey missing.");

        signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);
    }

    public ValueTask<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (renewalLock)
        {
            var nowUtc = timeProvider.GetUtcNow();
            if (cachedToken is null || nowUtc >= cachedTokenExpiresOnUtc - RenewBefore)
            {
                var expiresOnUtc = nowUtc.Add(TokenLifetime);
                cachedToken = Mint(nowUtc, expiresOnUtc);
                cachedTokenExpiresOnUtc = expiresOnUtc;
            }

            return ValueTask.FromResult(cachedToken);
        }
    }

    private string Mint(DateTimeOffset nowUtc, DateTimeOffset expiresOnUtc)
    {
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, ServiceAuthenticationDefaults.ServiceSubject),
                new Claim(ClaimTypes.Role, ServiceAuthenticationDefaults.ServiceAccountRole),
            ],
            notBefore: nowUtc.UtcDateTime,
            expires: expiresOnUtc.UtcDateTime,
            signingCredentials: signingCredentials);

        return tokenHandler.WriteToken(token);
    }
}
