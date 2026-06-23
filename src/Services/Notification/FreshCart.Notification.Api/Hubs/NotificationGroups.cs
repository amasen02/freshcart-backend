using System.Globalization;

namespace FreshCart.Notification.Api.Hubs;

/// <summary>
/// Pure resolver for the SignalR group names a connection joins. Kept free of hub plumbing so the
/// routing rules can be unit tested in isolation: every customer joins their own
/// <c>user:{userId}</c> group, and back-office roles additionally join <c>backoffice</c> to receive
/// the live sales-dashboard tick.
/// </summary>
public static class NotificationGroups
{
    public const string BackOffice = "backoffice";

    private const string UserGroupPrefix = "user:";

    private const string AdministratorRoleName = "Administrator";
    private const string ManagerRoleName = "Manager";
    private const string SupportAgentRoleName = "SupportAgent";

    private static readonly string[] BackOfficeRoles =
        [AdministratorRoleName, ManagerRoleName, SupportAgentRoleName];

    public static string ForUser(Guid userId) =>
        string.Create(CultureInfo.InvariantCulture, $"{UserGroupPrefix}{userId}");

    public static IReadOnlyList<string> ForRoles(IEnumerable<string> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);

        var joinsBackOffice = roles.Any(role =>
            BackOfficeRoles.Contains(role, StringComparer.Ordinal));

        return joinsBackOffice ? [BackOffice] : [];
    }
}
