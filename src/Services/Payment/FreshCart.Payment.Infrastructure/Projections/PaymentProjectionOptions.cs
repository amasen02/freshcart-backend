namespace FreshCart.Payment.Infrastructure.Projections;

/// <summary>
/// Tuneable options for the <see cref="PaymentReadModelProjectorService"/>. Bound from the
/// <c>PaymentProjection</c> configuration section.
/// </summary>
public sealed class PaymentProjectionOptions
{
    public const string SectionName = "PaymentProjection";

    public int BatchSize { get; init; } = 100;

    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(2);
}
