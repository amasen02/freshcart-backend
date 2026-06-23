using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Identity.Application.Common.Models;
using FreshCart.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace FreshCart.Identity.Application.Account.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(
    UserManager<ApplicationUser> userManager)
    : IQueryHandler<GetCurrentUserQuery, AuthenticationProfile>
{
    public async Task<AuthenticationProfile> Handle(
        GetCurrentUserQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var user = await userManager.FindByIdAsync(query.UserId.ToString()).ConfigureAwait(false)
            ?? throw new NotFoundException("ApplicationUser", query.UserId);

        var assignedRoles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        var multiFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false);

        return new AuthenticationProfile(
            UserId: user.Id,
            Email: user.Email ?? string.Empty,
            DisplayName: user.DisplayName,
            Roles: assignedRoles.ToArray(),
            MultiFactorEnabled: multiFactorEnabled);
    }
}
