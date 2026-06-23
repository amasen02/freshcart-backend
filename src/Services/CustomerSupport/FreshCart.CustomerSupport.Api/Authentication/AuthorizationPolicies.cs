namespace FreshCart.CustomerSupport.Api.Authentication;

/// <summary>
/// Authorization policies this service uses, and nothing more. Customers open chats, agents work
/// them, back-office staff audit them; each maps to one policy.
/// </summary>
public static class AuthorizationPolicies
{
    public const string Customer = "Customer";

    public const string SupportAgent = "SupportAgent";

    public const string CustomerOrSupportAgent = "CustomerOrSupportAgent";

    public const string BackOfficeUser = "BackOfficeUser";

    public const string CustomerRole = "Customer";

    public const string SupportAgentRole = "SupportAgent";

    public const string ManagerRole = "Manager";

    public const string AdministratorRole = "Administrator";
}
