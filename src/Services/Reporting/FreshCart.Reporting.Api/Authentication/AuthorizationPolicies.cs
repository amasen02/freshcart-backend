namespace FreshCart.Reporting.Api.Authentication;

/// <summary>
/// The single policy this back-office service uses. Every reporting route is staff-only, so the
/// <c>BackOfficeUser</c> policy admits the Administrator, SupportAgent and Manager roles. Both the
/// policy key and the role names are constants so a typo cannot silently open or close access.
/// </summary>
public static class AuthorizationPolicies
{
    public const string BackOfficeUser = "BackOfficeUser";

    public const string AdministratorRoleName = "Administrator";
    public const string SupportAgentRoleName = "SupportAgent";
    public const string ManagerRoleName = "Manager";
}
