namespace FreshCart.Payment.Api.Endpoints;

public sealed record RefundPaymentRequest(decimal Amount, string Reason);
