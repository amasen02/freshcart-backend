namespace FreshCart.Payment.Tests.Common;

/// <summary>
/// Deterministic clock for handler tests: every call to <see cref="GetUtcNow"/> returns the same
/// instant, so event timestamps are assertable values instead of moving targets.
/// </summary>
public sealed class FixedTimeProvider(DateTimeOffset fixedUtcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => fixedUtcNow;
}
