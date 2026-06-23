namespace FreshCart.Payment.Application.Abstractions;

public sealed record ProviderRefundResult(bool IsApproved, string? DeclineReason)
{
    public static ProviderRefundResult Approved() => new(true, null);

    public static ProviderRefundResult Declined(string declineReason) => new(false, declineReason);
}
