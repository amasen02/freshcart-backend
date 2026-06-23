using System.Security.Claims;
using FreshCart.Gateway.Yarp.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace FreshCart.Gateway.Tests.Auth;

public sealed class DownstreamAuthorizationPolicyTests
{
    [Fact]
    public void CookieAuthenticatedRequestWithoutAnAuthorizationHeaderIsExchanged()
    {
        var httpContext = CreateAuthenticatedContext();

        DownstreamAuthorizationPolicy.ShouldExchangeCookieForBearerToken(httpContext).Should().BeTrue();
    }

    [Fact]
    public void RequestThatAlreadyCarriesABearerTokenPassesThroughUntouched()
    {
        var httpContext = CreateAuthenticatedContext();
        httpContext.Request.Headers.Authorization = "Bearer programmatic.client.token";

        DownstreamAuthorizationPolicy.ShouldExchangeCookieForBearerToken(httpContext).Should().BeFalse();
    }

    [Fact]
    public void AnonymousRequestIsNotExchanged()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity()),
        };

        DownstreamAuthorizationPolicy.ShouldExchangeCookieForBearerToken(httpContext).Should().BeFalse();
    }

    [Fact]
    public void AuthorizationHeaderValueIsTheBearerScheme()
    {
        DownstreamAuthorizationPolicy.BuildAuthorizationHeaderValue("the.signed.jwt")
            .Should().Be("Bearer the.signed.jwt");
    }

    private static DefaultHttpContext CreateAuthenticatedContext()
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
            CookieAuthenticationDefaults.AuthenticationScheme);

        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity),
        };
    }
}
