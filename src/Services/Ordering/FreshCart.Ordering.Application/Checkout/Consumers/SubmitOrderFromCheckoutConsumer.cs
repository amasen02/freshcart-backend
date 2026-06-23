using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Ordering.Application.Abstractions;
using FreshCart.Ordering.Application.Checkout.Commands;
using FreshCart.Ordering.Domain.Orders;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FreshCart.Ordering.Application.Checkout.Consumers;

/// <summary>
/// Persists the Order aggregate from the checkout payload. Saving the aggregate also drains its
/// domain events into the outbox in the same transaction, so OrderPlaced is published exactly once
/// the row exists. The consumer is idempotent: a redelivered command finds the order already there
/// and returns without raising a second OrderPlaced.
/// </summary>
public sealed partial class SubmitOrderFromCheckoutConsumer(
    IOrderRepository orderRepository,
    ILogger<SubmitOrderFromCheckoutConsumer> logger) : IConsumer<SubmitOrderFromCheckout>
{
    public async Task Consume(ConsumeContext<SubmitOrderFromCheckout> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var checkout = context.Message.Checkout;

        var alreadyPersisted = await orderRepository
            .ExistsAsync(checkout.OrderId, context.CancellationToken)
            .ConfigureAwait(false);

        if (alreadyPersisted)
        {
            LogSkippingAlreadyPersistedOrder(checkout.OrderId);
            return;
        }

        var order = Order.Submit(MapToSubmission(checkout));

        await orderRepository.AddAsync(order, context.CancellationToken).ConfigureAwait(false);
        await orderRepository.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

        LogOrderSubmitted(order.Id, order.CustomerId, order.GrandTotal.Amount, order.GrandTotal.CurrencyCode);
    }

    private static OrderSubmission MapToSubmission(BasketCheckoutStartedIntegrationEvent checkout) => new()
    {
        OrderId = checkout.OrderId,
        CustomerId = checkout.CustomerId,
        CustomerEmail = checkout.CustomerEmail,
        CustomerDisplayName = checkout.CustomerDisplayName,
        PaymentMethod = checkout.PaymentMethod,
        Subtotal = new Money(checkout.Subtotal, checkout.CurrencyCode),
        DiscountTotal = new Money(checkout.DiscountTotal, checkout.CurrencyCode),
        TaxTotal = new Money(checkout.TaxTotal, checkout.CurrencyCode),
        ShippingTotal = new Money(checkout.ShippingTotal, checkout.CurrencyCode),
        GrandTotal = new Money(checkout.GrandTotal, checkout.CurrencyCode),
        BillingAddress = MapAddress(checkout.BillingAddress),
        ShippingAddress = checkout.ShippingAddress is null ? null : MapAddress(checkout.ShippingAddress),
        Lines = [.. checkout.Lines.Select(line => new OrderLine(
            line.ProductId,
            line.ProductSku,
            line.ProductName,
            line.PrimaryCategory,
            new Money(line.UnitPrice, checkout.CurrencyCode),
            line.Quantity,
            line.IsDigital))],
        SubmittedOnUtc = checkout.OccurredOnUtc,
    };

    private static Address MapAddress(CheckoutAddress address) =>
        new(address.Line1, address.Line2, address.City, address.PostalCode, address.CountryCode);

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Skipping already persisted order {OrderId}")]
    private partial void LogSkippingAlreadyPersistedOrder(Guid orderId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Submitted order {OrderId} for customer {CustomerId} totalling {GrandTotal} {CurrencyCode}")]
    private partial void LogOrderSubmitted(Guid orderId, Guid customerId, decimal grandTotal, string currencyCode);
}
