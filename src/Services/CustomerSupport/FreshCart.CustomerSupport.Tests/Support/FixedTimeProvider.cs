namespace FreshCart.CustomerSupport.Tests.Support;

/// <summary>
/// Deterministic clock so a session's start time and a message's send time are assertable values
/// rather than moving targets.
/// </summary>
public sealed class FixedTimeProvider(DateTimeOffset fixedUtcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => fixedUtcNow;
}
