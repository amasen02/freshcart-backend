using FreshCart.Payment.Domain;

namespace FreshCart.Payment.Application.Payments.Commands.CapturePayment;

public sealed record CapturePaymentResult(
    Guid PaymentId,
    Guid OrderId,
    PaymentStatus Status,
    string? FailureReason);
