namespace FreshCart.Pricing.Tests.Support;

/// <summary>Deterministic clock for tests; always returns the instant it was constructed with.</summary>
public sealed class FixedTimeProvider(DateTimeOffset fixedUtcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => fixedUtcNow;
}
