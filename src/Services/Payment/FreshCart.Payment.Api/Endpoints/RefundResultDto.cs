using FreshCart.Payment.Application.Payments.Commands.RefundPayment;

namespace FreshCart.Payment.Api.Endpoints;

public sealed record RefundResultDto(
    Guid PaymentId,
    Guid OrderId,
    string Status,
    decimal RefundedAmount)
{
    public static RefundResultDto FromCommandResult(RefundPaymentResult commandResult)
    {
        ArgumentNullException.ThrowIfNull(commandResult);

        return new RefundResultDto(
            commandResult.PaymentId,
            commandResult.OrderId,
            commandResult.Status.ToString(),
            commandResult.RefundedAmount);
    }
}
