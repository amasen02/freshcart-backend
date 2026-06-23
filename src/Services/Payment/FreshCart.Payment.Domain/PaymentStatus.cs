namespace FreshCart.Payment.Domain;

public enum PaymentStatus
{
    Initiated,
    Authorized,
    Captured,
    Declined,
    Refunded,
    PartiallyRefunded,
}
