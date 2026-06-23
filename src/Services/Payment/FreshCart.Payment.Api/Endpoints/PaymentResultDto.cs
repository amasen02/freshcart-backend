using FreshCart.Payment.Application.Payments.Commands.CapturePayment;

namespace FreshCart.Payment.Api.Endpoints;

public sealed record PaymentResultDto(
    Guid PaymentId,
    Guid OrderId,
    string Status,
    string? FailureReason)
{
    public static PaymentResultDto FromCommandResult(CapturePaymentResult commandResult)
    {
        ArgumentNullException.ThrowIfNull(commandResult);

        return new PaymentResultDto(
            commandResult.PaymentId,
            commandResult.OrderId,
            commandResult.Status.ToString(),
            commandResult.FailureReason);
    }
}
