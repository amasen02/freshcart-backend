namespace FreshCart.Catalog.Tests.TestInfrastructure;

/// <summary>
/// Deterministic clock for handler tests; always reports the instant it was constructed with.
/// </summary>
internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
