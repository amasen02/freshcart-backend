namespace FreshCart.Basket.Api.Authentication;

/// <summary>
/// The single policy this service uses. Every basket route is customer-owned data, so everything
/// requires the Customer role; back-office roles have no business inside another person's basket.
/// </summary>
public static class AuthorizationPolicies
{
    public const string Customer = "Customer";

    public const string CustomerRoleName = "Customer";
}
