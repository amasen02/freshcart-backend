namespace FreshCart.Ordering.Tests.Support;

/// <summary>
/// Deterministic clock for tests that assert on timestamps. The instant is fixed at construction so
/// confirmation and cancellation times are predictable.
/// </summary>
public sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
