using System.Text.Json;
using FreshCart.Basket.Api.Domain;
using FreshCart.Basket.Api.Persistence;
using FreshCart.Basket.Api.Pricing;
using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.BuildingBlocks.Messaging.Outbox;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.Checkout;

/// <summary>
/// Turns the live basket into an order request. The basket is repriced through Pricing first
/// (stored prices are display snapshots, never settlement values), then the checkout event, the
/// archive snapshot and the live-basket deletion ride one Marten transaction via the unit of work.
/// Losing the event after that commit is impossible: the outbox publisher drains it from the same
/// database.
/// </summary>
public sealed class CheckoutCommandHandler(
    IBasketRepository basketRepository,
    IBasketPricingClient basketPricingClient,
    IBasketUnitOfWork basketUnitOfWork,
    IBasketCacheInvalidator basketCacheInvalidator,
    TimeProvider timeProvider)
    : ICommandHandler<CheckoutCommand, CheckoutResult>
{
    private static readonly JsonSerializerOptions OutboxSerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<CheckoutResult> Handle(CheckoutCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var customerBasket = await basketRepository.GetAsync(command.CustomerId, cancellationToken).ConfigureAwait(false);
        if (customerBasket is null || customerBasket.IsEmpty)
        {
            throw new BadRequestException("Cannot check out an empty basket.");
        }

        var livePricing = await basketPricingClient
            .PriceBasketAsync(BasketPricingRequest.ForBasket(customerBasket), cancellationToken)
            .ConfigureAwait(false);

        var orderId = Guid.CreateVersion7();
        var shippingTotal = customerBasket.ContainsPhysicalItems
            ? BasketDefaults.StandardShippingFee
            : decimal.Zero;

        var checkoutStartedEvent = BuildCheckoutStartedEvent(command, customerBasket, livePricing, orderId, shippingTotal);
        var checkoutOutboxMessage = new OutboxMessage
        {
            EventType = checkoutStartedEvent.EventType,
            ContentJson = JsonSerializer.Serialize(checkoutStartedEvent, OutboxSerializerOptions),
            OccurredOnUtc = timeProvider.GetUtcNow(),
        };

        var archivedBasket = BuildArchiveSnapshot(customerBasket, livePricing, orderId, shippingTotal);

        await basketUnitOfWork
            .CommitCheckoutAsync(archivedBasket, checkoutOutboxMessage, command.CustomerId, cancellationToken)
            .ConfigureAwait(false);

        await basketCacheInvalidator.InvalidateAsync(command.CustomerId, cancellationToken).ConfigureAwait(false);

        return new CheckoutResult(orderId);
    }

    private static BasketCheckoutStartedIntegrationEvent BuildCheckoutStartedEvent(
        CheckoutCommand command,
        ShoppingBasket customerBasket,
        BasketPricingResult livePricing,
        Guid orderId,
        decimal shippingTotal) => new()
    {
        OrderId = orderId,
        CustomerId = command.CustomerId,
        CustomerEmail = command.CustomerEmail,
        CustomerDisplayName = command.CustomerDisplayName,
        CurrencyCode = customerBasket.CurrencyCode,
        PaymentMethod = command.PaymentMethod,
        CouponCode = livePricing.AppliedCoupon,
        Subtotal = livePricing.Subtotal,
        DiscountTotal = livePricing.DiscountTotal,
        TaxTotal = livePricing.TaxTotal,
        ShippingTotal = shippingTotal,
        GrandTotal = livePricing.GrandTotal + shippingTotal,
        BillingAddress = command.BillingAddress,
        ShippingAddress = command.ShippingAddress,
        Lines = BuildCheckoutLines(customerBasket, livePricing),
    };

    private static IReadOnlyList<CheckoutLine> BuildCheckoutLines(
        ShoppingBasket customerBasket,
        BasketPricingResult livePricing)
    {
        var pricedLinesByProductId = livePricing.Lines.ToDictionary(pricedLine => pricedLine.ProductId);

        return [.. customerBasket.Items.Select(basketItem =>
        {
            if (!pricedLinesByProductId.TryGetValue(basketItem.ProductId, out var pricedLine))
            {
                throw new InternalServerException(
                    $"Pricing response is missing a line for product \"{basketItem.ProductId}\".");
            }

            return new CheckoutLine(
                basketItem.ProductId,
                basketItem.ProductSku,
                basketItem.ProductName,
                basketItem.PrimaryCategory,
                pricedLine.UnitPrice,
                basketItem.Quantity,
                basketItem.IsDigital);
        })];
    }

    private ArchivedBasket BuildArchiveSnapshot(
        ShoppingBasket customerBasket,
        BasketPricingResult livePricing,
        Guid orderId,
        decimal shippingTotal) => new()
    {
        Id = orderId,
        CustomerId = customerBasket.Id,
        CurrencyCode = customerBasket.CurrencyCode,
        Items = [.. customerBasket.Items],
        CouponCode = livePricing.AppliedCoupon,
        Subtotal = livePricing.Subtotal,
        DiscountTotal = livePricing.DiscountTotal,
        TaxTotal = livePricing.TaxTotal,
        ShippingTotal = shippingTotal,
        GrandTotal = livePricing.GrandTotal + shippingTotal,
        CheckedOutOnUtc = timeProvider.GetUtcNow(),
    };
}
