using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FreshCart.Identity.Api.Endpoints.Account;
using FreshCart.Identity.Api.Endpoints.Authentication;
using FreshCart.Identity.Tests.Common;

namespace FreshCart.Identity.Tests.Authentication;

/// <summary>
/// End-to-end tests of the sign-up + sign-in cookie flow. Validates that a freshly created customer
/// account can sign in and that the session cookie is then accepted on a protected endpoint.
/// </summary>
public sealed class SignUpAndSignInIntegrationTests(IdentityApiFactory identityApiFactory)
    : IClassFixture<IdentityApiFactory>
{
    [Fact]
    public async Task SignUpThenSignInIssuesSessionCookieAndAllowsProtectedAccess()
    {
        // The session cookie is Secure + SameSite=Strict, so the client must talk HTTPS.
        var httpClient = identityApiFactory.CreateDefaultClient(new Uri("https://localhost"));

        var signUpPayload = new SignUpRequest(
            Email: $"customer-{Guid.NewGuid():N}@freshcart.test",
            Password: "Sup3rSecret!Passphrase",
            DisplayName: "Test Customer",
            MarketingConsent: false,
            SignInImmediately: false,
            UseCookie: false);

        var signUpResponse = await httpClient.PostAsJsonAsync("/auth/sign-up", signUpPayload);

        signUpResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var signInPayload = new SignInRequest(
            Email: signUpPayload.Email,
            Password: signUpPayload.Password,
            MultiFactorCode: null,
            UseCookie: true,
            RememberMe: true);

        var signInResponse = await httpClient.PostAsJsonAsync("/auth/sign-in", signInPayload);

        signInResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        signInResponse.Headers
            .GetValues("Set-Cookie")
            .Should().Contain(cookieValue => cookieValue.Contains("FreshCart.Session", StringComparison.Ordinal));

        // The session cookie is replayed explicitly so the assertion proves that exactly this
        // cookie, and not any other ambient state, grants access to the protected endpoint.
        var sessionCookiePair = signInResponse.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith("FreshCart.Session=", StringComparison.Ordinal))
            .Split(';')[0];
        using var currentUserRequest = new HttpRequestMessage(HttpMethod.Get, new Uri("/account/me", UriKind.Relative));
        currentUserRequest.Headers.Add("Cookie", sessionCookiePair);
        var currentUserResponse = await httpClient.SendAsync(currentUserRequest);

        currentUserResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var currentUser = await currentUserResponse.Content.ReadFromJsonAsync<CurrentUserResponse>();

        currentUser.Should().NotBeNull();
        currentUser!.Email.Should().Be(signUpPayload.Email);
        currentUser.Roles.Should().Contain("Customer");
    }
}
