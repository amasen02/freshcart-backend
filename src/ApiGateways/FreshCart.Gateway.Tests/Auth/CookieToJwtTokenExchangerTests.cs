using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FreshCart.Gateway.Tests.Support;
using FreshCart.Gateway.Yarp.Auth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FreshCart.Gateway.Tests.Auth;

public sealed class CookieToJwtTokenExchangerTests
{
    private const string SigningKey = "gateway-unit-test-signing-key-please-replace-32+chars-min";
    private const string Issuer = "https://freshcart.local/identity";
    private const string Audience = "https://freshcart.local";

    private static readonly DateTimeOffset FixedNowUtc = new(2026, 6, 18, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid CustomerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void MintedTokenCarriesSubjectEmailDisplayNameAndEveryRole()
    {
        var exchanger = new CookieToJwtTokenExchanger(
            CreateRealSigner(),
            new MemoryCache(new MemoryCacheOptions()),
            new MutableTimeProvider(FixedNowUtc));

        var cookiePrincipal = CookiePrincipalFactory.Create(
            CustomerId,
            "shopper@freshcart.local",
            "Ama Shopper",
            "1718700000",
            "Customer",
            "Manager");

        var token = new JwtSecurityTokenHandler().ReadJwtToken(exchanger.ExchangeForBearerToken(cookiePrincipal));

        token.Issuer.Should().Be(Issuer);
        token.Audiences.Should().ContainSingle().Which.Should().Be(Audience);
        token.Subject.Should().Be(CustomerId.ToString());
        token.Claims.Should().ContainSingle(claim => string.Equals(claim.Type, DownstreamTokenClaim.Email, StringComparison.Ordinal))
            .Which.Value.Should().Be("shopper@freshcart.local");
        token.Claims.Should().ContainSingle(claim => string.Equals(claim.Type, DownstreamTokenClaim.DisplayName, StringComparison.Ordinal))
            .Which.Value.Should().Be("Ama Shopper");
        token.Claims.Where(claim => string.Equals(claim.Type, DownstreamTokenClaim.Role, StringComparison.Ordinal))
            .Select(claim => claim.Value)
            .Should().BeEquivalentTo("Customer", "Manager");
    }

    [Fact]
    public void MintedTokenExpiresFiveMinutesAfterTheIssuingInstant()
    {
        var exchanger = new CookieToJwtTokenExchanger(
            CreateRealSigner(),
            new MemoryCache(new MemoryCacheOptions()),
            new MutableTimeProvider(FixedNowUtc));

        var cookiePrincipal = CookiePrincipalFactory.Create(
            CustomerId,
            "shopper@freshcart.local",
            "Ama Shopper",
            "1718700000",
            "Customer");

        var token = new JwtSecurityTokenHandler().ReadJwtToken(exchanger.ExchangeForBearerToken(cookiePrincipal));

        token.ValidTo.Should().BeCloseTo(FixedNowUtc.UtcDateTime.AddMinutes(5), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SecondExchangeForTheSameSessionReturnsTheCachedTokenWithoutResigning()
    {
        var signer = Substitute.For<IDownstreamTokenSigner>();
        signer
            .SignToken(Arg.Any<DownstreamPrincipal>(), Arg.Any<DateTimeOffset>())
            .Returns("signed-token");

        var exchanger = new CookieToJwtTokenExchanger(
            signer,
            new MemoryCache(new MemoryCacheOptions()),
            new MutableTimeProvider(FixedNowUtc));

        var cookiePrincipal = CookiePrincipalFactory.Create(
            CustomerId,
            "shopper@freshcart.local",
            "Ama Shopper",
            "1718700000",
            "Customer");

        var firstToken = exchanger.ExchangeForBearerToken(cookiePrincipal);
        var secondToken = exchanger.ExchangeForBearerToken(cookiePrincipal);

        firstToken.Should().Be("signed-token");
        secondToken.Should().Be(firstToken);
        signer.Received(1).SignToken(Arg.Any<DownstreamPrincipal>(), Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public void ReauthenticatedSessionMintsAFreshTokenInsteadOfReplayingTheCachedOne()
    {
        var signer = Substitute.For<IDownstreamTokenSigner>();
        signer
            .SignToken(Arg.Any<DownstreamPrincipal>(), Arg.Any<DateTimeOffset>())
            .Returns(callInfo => $"token-for-{callInfo.Arg<DownstreamPrincipal>().Subject}-{Guid.NewGuid()}");

        var exchanger = new CookieToJwtTokenExchanger(
            signer,
            new MemoryCache(new MemoryCacheOptions()),
            new MutableTimeProvider(FixedNowUtc));

        var firstSession = CookiePrincipalFactory.Create(
            CustomerId, "shopper@freshcart.local", "Ama Shopper", "1718700000", "Customer");
        var reauthenticatedSession = CookiePrincipalFactory.Create(
            CustomerId, "shopper@freshcart.local", "Ama Shopper", "1718800000", "Customer");

        var firstToken = exchanger.ExchangeForBearerToken(firstSession);
        var secondToken = exchanger.ExchangeForBearerToken(reauthenticatedSession);

        secondToken.Should().NotBe(firstToken);
        signer.Received(2).SignToken(Arg.Any<DownstreamPrincipal>(), Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public void ExchangeThrowsWhenThePrincipalCarriesNoSubject()
    {
        var exchanger = new CookieToJwtTokenExchanger(
            CreateRealSigner(),
            new MemoryCache(new MemoryCacheOptions()),
            new MutableTimeProvider(FixedNowUtc));

        var anonymousPrincipal = new ClaimsPrincipal(new ClaimsIdentity());

        var exchange = () => exchanger.ExchangeForBearerToken(anonymousPrincipal);

        exchange.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ExchangeThrowsWhenThePrincipalCarriesNoAuthenticationInstantSoNoReplayableKeyIsBuilt()
    {
        var exchanger = new CookieToJwtTokenExchanger(
            CreateRealSigner(),
            new MemoryCache(new MemoryCacheOptions()),
            new MutableTimeProvider(FixedNowUtc));

        var principalWithoutAuthenticationInstant = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, CustomerId.ToString())]));

        var exchange = () => exchanger.ExchangeForBearerToken(principalWithoutAuthenticationInstant);

        exchange.Should().Throw<InvalidOperationException>();
    }

    private static HmacDownstreamTokenSigner CreateRealSigner()
    {
        var jwtOptions = Options.Create(new GatewayJwtOptions
        {
            Issuer = Issuer,
            Audience = Audience,
            SigningKey = SigningKey,
        });

        return new HmacDownstreamTokenSigner(jwtOptions);
    }
}
