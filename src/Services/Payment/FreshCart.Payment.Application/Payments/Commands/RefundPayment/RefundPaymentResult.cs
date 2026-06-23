using FreshCart.Payment.Domain;

namespace FreshCart.Payment.Application.Payments.Commands.RefundPayment;

public sealed record RefundPaymentResult(
    Guid PaymentId,
    Guid OrderId,
    PaymentStatus Status,
    decimal RefundedAmount);
