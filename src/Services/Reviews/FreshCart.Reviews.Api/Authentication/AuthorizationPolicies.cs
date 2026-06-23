namespace FreshCart.Reviews.Api.Authentication;

/// <summary>
/// The authorization policies this service uses, and the role names they are built from. Customers write
/// and read their own reviews; back-office staff moderate. Nothing else is defined here, so the surface
/// stays exactly as wide as the endpoints require.
/// </summary>
public static class AuthorizationPolicies
{
    public const string Customer = nameof(Customer);
    public const string BackOfficeUser = nameof(BackOfficeUser);

    public const string CustomerRole = "Customer";
    public const string AdministratorRole = "Administrator";
    public const string SupportAgentRole = "SupportAgent";
    public const string ManagerRole = "Manager";
}
