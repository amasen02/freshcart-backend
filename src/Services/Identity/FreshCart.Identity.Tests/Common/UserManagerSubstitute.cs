using FreshCart.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace FreshCart.Identity.Tests.Common;

/// <summary>
/// Builds an NSubstitute proxy over <see cref="UserManager{TUser}"/>. Its virtual method surface is
/// the seam ASP.NET Identity provides for testing; the eight optional collaborators are never touched
/// when every member under test is stubbed, so only the user store needs a substitute.
/// </summary>
internal static class UserManagerSubstitute
{
    public static UserManager<ApplicationUser> Create() =>
        Substitute.For<UserManager<ApplicationUser>>(
            Substitute.For<IUserStore<ApplicationUser>>(),
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
}
