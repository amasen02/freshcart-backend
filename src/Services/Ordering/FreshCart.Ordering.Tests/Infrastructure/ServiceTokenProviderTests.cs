using System.Text;
using System.Text.Json;
using FreshCart.BuildingBlocks.Security;
using FreshCart.Ordering.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FreshCart.Ordering.Tests.Infrastructure;

public sealed class ServiceTokenProviderTests
{
    private const string Issuer = "https://freshcart.local/identity";
    private const string Audience = "https://freshcart.local";
    private const string SigningKey = "service-token-provider-test-signing-key-32-bytes-minimum!!";

    private static ServiceTokenProvider CreateProvider(TimeProvider timeProvider)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
                ["Jwt:SigningKey"] = SigningKey,
            })
            .Build();

        return new ServiceTokenProvider(configuration, timeProvider);
    }

    [Fact]
    public async Task TheMintedTokenCarriesTheServiceSubjectRoleIssuerAndAudience()
    {
        var provider = CreateProvider(TimeProvider.System);

        var token = await provider.GetTokenAsync(CancellationToken.None);
        var payload = DecodeJwtPayload(token);

        payload.GetProperty("iss").GetString().Should().Be(Issuer);
        payload.GetProperty("aud").GetString().Should().Be(Audience);
        payload.GetProperty("sub").GetString().Should().Be(ServiceAuthenticationDefaults.ServiceSubject);
        payload.GetRawText().Should().Contain(ServiceAuthenticationDefaults.ServiceAccountRole,
            "the role claim the downstream ServiceCaller policy requires must be present in the decoded payload");
        payload.TryGetProperty("exp", out _).Should().BeTrue("the service token must expire");
    }

    [Fact]
    public async Task TheTokenIsCachedAndReusedWhileItIsStillFresh()
    {
        var provider = CreateProvider(TimeProvider.System);

        var first = await provider.GetTokenAsync(CancellationToken.None);
        var second = await provider.GetTokenAsync(CancellationToken.None);

        second.Should().Be(first, "a still-fresh token must be reused rather than re-minted on every call");
    }

    private static JsonElement DecodeJwtPayload(string token)
    {
        var segments = token.Split('.');
        segments.Should().HaveCount(3, "a signed JWT has header.payload.signature segments");

        var payloadSegment = segments[1].Replace('-', '+').Replace('_', '/');
        var padded = payloadSegment.PadRight(payloadSegment.Length + ((4 - (payloadSegment.Length % 4)) % 4), '=');
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(padded));

        return JsonDocument.Parse(json).RootElement.Clone();
    }
}
