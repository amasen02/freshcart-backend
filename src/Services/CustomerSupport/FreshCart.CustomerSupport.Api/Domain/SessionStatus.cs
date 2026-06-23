namespace FreshCart.CustomerSupport.Api.Domain;

/// <summary>
/// Lifecycle of a support chat session. A session starts either <see cref="Queued"/> (no agent free)
/// or <see cref="Active"/> (assigned on creation) and always finishes <see cref="Ended"/>.
/// </summary>
public enum SessionStatus
{
    Queued = 0,
    Active = 1,
    Ended = 2,
}
