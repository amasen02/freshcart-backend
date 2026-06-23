namespace FreshCart.CustomerSupport.Api.Domain;

/// <summary>
/// Identifies which side of a conversation authored a message. Persisted alongside the message so a
/// transcript renders correctly without re-deriving the role from the participant identifiers.
/// </summary>
public enum SenderRole
{
    Customer = 0,
    Agent = 1,
}
