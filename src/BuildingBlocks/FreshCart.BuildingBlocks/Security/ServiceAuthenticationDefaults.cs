namespace FreshCart.BuildingBlocks.Security;

/// <summary>
/// Shared constants for service-to-service (machine) authentication. A calling service such as the
/// Ordering saga mints a short-lived JWT carrying <see cref="ServiceAccountRole"/>; internal endpoints
/// that must only ever be reached by another service (never a browser user) guard with the
/// <see cref="ServiceCallerPolicy"/> authorization policy.
/// </summary>
public static class ServiceAuthenticationDefaults
{
    public const string ServiceAccountRole = "ServiceAccount";

    public const string ServiceCallerPolicy = "ServiceCaller";

    public const string ServiceSubject = "freshcart-service";
}
