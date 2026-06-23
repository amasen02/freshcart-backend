namespace FreshCart.Gateway.Tests.Support;

/// <summary>
/// Deterministic clock for the token-exchange tests. The current instant is settable so a test can
/// advance time and assert the cache behaviour without sleeping.
/// </summary>
public sealed class MutableTimeProvider : TimeProvider
{
    private DateTimeOffset currentUtcNow;

    public MutableTimeProvider(DateTimeOffset initialUtcNow) => currentUtcNow = initialUtcNow;

    public override DateTimeOffset GetUtcNow() => currentUtcNow;

    public void Advance(TimeSpan delta) => currentUtcNow = currentUtcNow.Add(delta);
}
