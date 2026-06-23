using System.Globalization;
using FluentAssertions;
using FreshCart.Notification.Api.Hubs;
using Xunit;

namespace FreshCart.Notification.Tests.Hubs;

public sealed class NotificationGroupsTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void ForUserNamesTheGroupWithTheUserScopedPrefix()
    {
        var groupName = NotificationGroups.ForUser(UserId);

        groupName.Should().Be(string.Create(CultureInfo.InvariantCulture, $"user:{UserId}"));
    }

    [Theory]
    [InlineData("Administrator")]
    [InlineData("Manager")]
    [InlineData("SupportAgent")]
    public void BackOfficeRolesJoinTheBackOfficeGroup(string backOfficeRole)
    {
        var groups = NotificationGroups.ForRoles([backOfficeRole]);

        groups.Should().ContainSingle().Which.Should().Be(NotificationGroups.BackOffice);
    }

    [Fact]
    public void CustomerRoleAloneJoinsNoBroadcastGroup()
    {
        var groups = NotificationGroups.ForRoles(["Customer"]);

        groups.Should().BeEmpty();
    }

    [Fact]
    public void AUserWithBothCustomerAndAManagementRoleStillJoinsBackOffice()
    {
        var groups = NotificationGroups.ForRoles(["Customer", "Manager"]);

        groups.Should().ContainSingle().Which.Should().Be(NotificationGroups.BackOffice);
    }

    [Fact]
    public void NoRolesJoinsNoBroadcastGroup()
    {
        var groups = NotificationGroups.ForRoles([]);

        groups.Should().BeEmpty();
    }
}
