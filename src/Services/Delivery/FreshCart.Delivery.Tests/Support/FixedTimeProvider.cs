namespace FreshCart.Delivery.Tests.Support;

/// <summary>
/// Minimal <see cref="TimeProvider"/> test double that always reports a fixed instant, so timestamp
/// assertions are exact. Mirrors the established Reporting test idiom rather than pulling in an extra
/// testing package.
/// </summary>
internal sealed class FixedTimeProvider(DateTimeOffset fixedNowUtc) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => fixedNowUtc;
}
