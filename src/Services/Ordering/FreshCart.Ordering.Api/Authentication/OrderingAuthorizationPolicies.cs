namespace FreshCart.Ordering.Api.Authentication;

/// <summary>
/// Policy and role names used by the Ordering endpoints. Customers manage their own orders;
/// administrators may view any order and issue refunds.
/// </summary>
public static class OrderingAuthorizationPolicies
{
    public const string Customer = nameof(Customer);

    public const string Administrator = nameof(Administrator);

    public const string CustomerRoleName = "Customer";

    public const string AdministratorRoleName = "Administrator";
}
