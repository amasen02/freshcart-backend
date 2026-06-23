namespace FreshCart.Basket.Api.Features.ShoppingBaskets.Checkout;

/// <summary>
/// Payment method names accepted at checkout. The value travels verbatim on
/// <c>BasketCheckoutStartedIntegrationEvent</c>, so the names here are a cross-service contract
/// with Ordering and Payment, not a local enum.
/// </summary>
public static class PaymentMethods
{
    public const string CreditCard = "CreditCard";

    public const string PayPal = "PayPal";

    public const string CashOnDelivery = "CashOnDelivery";

    public static readonly IReadOnlyList<string> All = [CreditCard, PayPal, CashOnDelivery];

    public static bool IsSupported(string paymentMethod) => All.Contains(paymentMethod, StringComparer.Ordinal);
}
