using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Payment.Application.Payments.Commands.CapturePayment;

public sealed record CapturePaymentCommand(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string CurrencyCode,
    string Method,
    string IdempotencyKey) : ICommand<CapturePaymentResult>;
