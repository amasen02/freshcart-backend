namespace FreshCart.Payment.Application.Abstractions;

public sealed record ProviderCaptureResult(bool IsApproved, string? DeclineReason)
{
    public static ProviderCaptureResult Approved() => new(true, null);

    public static ProviderCaptureResult Declined(string declineReason) => new(false, declineReason);
}
