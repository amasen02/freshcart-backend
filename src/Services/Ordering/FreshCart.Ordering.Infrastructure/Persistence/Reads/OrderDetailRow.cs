using FreshCart.Ordering.Application.Orders.Dtos;

namespace FreshCart.Ordering.Infrastructure.Persistence.Reads;

/// <summary>
/// Flat row Dapper materialises for the order header, including the two owned addresses split into
/// their column groups. Mapped into the nested <see cref="OrderDetailDto"/> once the lines are loaded.
/// </summary>
public sealed class OrderDetailRow
{
    public Guid OrderId { get; init; }

    public Guid CustomerId { get; init; }

    public string Status { get; init; } = string.Empty;

    public string CustomerEmail { get; init; } = string.Empty;

    public string CustomerDisplayName { get; init; } = string.Empty;

    public string PaymentMethod { get; init; } = string.Empty;

    public decimal Subtotal { get; init; }

    public decimal DiscountTotal { get; init; }

    public decimal TaxTotal { get; init; }

    public decimal ShippingTotal { get; init; }

    public decimal GrandTotal { get; init; }

    public string CurrencyCode { get; init; } = string.Empty;

    public string? FailureReason { get; init; }

    public DateTimeOffset SubmittedOnUtc { get; init; }

    public DateTimeOffset? ConfirmedOnUtc { get; init; }

    public DateTimeOffset? CancelledOnUtc { get; init; }

    public string BillingLine1 { get; init; } = string.Empty;

    public string? BillingLine2 { get; init; }

    public string BillingCity { get; init; } = string.Empty;

    public string BillingPostalCode { get; init; } = string.Empty;

    public string BillingCountryCode { get; init; } = string.Empty;

    public string? ShippingLine1 { get; init; }

    public string? ShippingLine2 { get; init; }

    public string? ShippingCity { get; init; }

    public string? ShippingPostalCode { get; init; }

    public string? ShippingCountryCode { get; init; }

    public OrderDetailDto ToDto(IReadOnlyList<OrderLineDto> lines) => new()
    {
        OrderId = OrderId,
        CustomerId = CustomerId,
        Status = Status,
        CustomerEmail = CustomerEmail,
        CustomerDisplayName = CustomerDisplayName,
        PaymentMethod = PaymentMethod,
        Subtotal = Subtotal,
        DiscountTotal = DiscountTotal,
        TaxTotal = TaxTotal,
        ShippingTotal = ShippingTotal,
        GrandTotal = GrandTotal,
        CurrencyCode = CurrencyCode,
        BillingAddress = new OrderAddressDto(
            BillingLine1,
            BillingLine2,
            BillingCity,
            BillingPostalCode,
            BillingCountryCode),
        ShippingAddress = BuildShippingAddress(),
        FailureReason = FailureReason,
        SubmittedOnUtc = SubmittedOnUtc,
        ConfirmedOnUtc = ConfirmedOnUtc,
        CancelledOnUtc = CancelledOnUtc,
        Lines = lines,
    };

    private OrderAddressDto? BuildShippingAddress()
    {
        if (string.IsNullOrEmpty(ShippingLine1)
            || ShippingCity is null
            || ShippingPostalCode is null
            || ShippingCountryCode is null)
        {
            return null;
        }

        return new OrderAddressDto(ShippingLine1, ShippingLine2, ShippingCity, ShippingPostalCode, ShippingCountryCode);
    }
}
