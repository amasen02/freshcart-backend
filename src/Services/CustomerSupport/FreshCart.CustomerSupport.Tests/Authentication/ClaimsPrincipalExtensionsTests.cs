using System.Security.Claims;
using FluentAssertions;
using FreshCart.CustomerSupport.Api.Authentication;
using Xunit;

namespace FreshCart.CustomerSupport.Tests.Authentication;

public sealed class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void IsCustomerIsTrueOnlyForACallerInTheCustomerRole()
    {
        var customer = PrincipalInRole(AuthorizationPolicies.CustomerRole);

        customer.IsCustomer().Should().BeTrue();
        customer.IsSupportAgent().Should().BeFalse();
    }

    [Fact]
    public void IsSupportAgentIsTrueOnlyForACallerInTheSupportAgentRole()
    {
        var agent = PrincipalInRole(AuthorizationPolicies.SupportAgentRole);

        agent.IsSupportAgent().Should().BeTrue();
        agent.IsCustomer().Should().BeFalse();
    }

    [Fact]
    public void BackOfficeManagerIsNeitherCustomerNorSupportAgentSoActiveSessionsForbidsThem()
    {
        var manager = PrincipalInRole(AuthorizationPolicies.ManagerRole);

        manager.IsCustomer().Should().BeFalse();
        manager.IsSupportAgent().Should().BeFalse();
    }

    [Fact]
    public void AdministratorIsNeitherCustomerNorSupportAgent()
    {
        var administrator = PrincipalInRole(AuthorizationPolicies.AdministratorRole);

        administrator.IsCustomer().Should().BeFalse();
        administrator.IsSupportAgent().Should().BeFalse();
        administrator.IsAdministrator().Should().BeTrue();
    }

    private static ClaimsPrincipal PrincipalInRole(string roleName) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Role, roleName)], authenticationType: "Test"));
}
