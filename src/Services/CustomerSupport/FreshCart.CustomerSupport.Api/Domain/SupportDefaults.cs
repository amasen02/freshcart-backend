namespace FreshCart.CustomerSupport.Api.Domain;

/// <summary>
/// Domain constants for the support service. Centralising them keeps the message-length rule, the
/// Redis key names and the role names from drifting between the hub, the registry and the endpoints.
/// </summary>
public static class SupportDefaults
{
    public const int MaxTopicLength = 120;

    public const int MaxMessageLength = 2_000;

    public const string SupportAgentRoleName = "SupportAgent";

    public const string AdministratorRoleName = "Administrator";
}
