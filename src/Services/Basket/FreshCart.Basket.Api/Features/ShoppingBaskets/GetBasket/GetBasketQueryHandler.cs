using FreshCart.Basket.Api.Domain;
using FreshCart.Basket.Api.Persistence;
using FreshCart.Basket.Api.Pricing;
using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.GetBasket;

/// <summary>
/// Reads the basket and reprices it through the Pricing service so the customer always sees live
/// totals; the unit prices stored on the document are display snapshots only.
/// </summary>
public sealed class GetBasketQueryHandler(
    IBasketRepository basketRepository,
    IBasketPricingClient basketPricingClient)
    : IQueryHandler<GetBasketQuery, BasketDto>
{
    private const int MoneyDecimalPlaces = 2;

    public async Task<BasketDto> Handle(GetBasketQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var customerBasket = await basketRepository.GetAsync(query.CustomerId, cancellationToken).ConfigureAwait(false);
        if (customerBasket is null || customerBasket.IsEmpty)
        {
            return BasketDto.EmptyFor(query.CustomerId);
        }

        var livePricing = await basketPricingClient
            .PriceBasketAsync(BasketPricingRequest.ForBasket(customerBasket), cancellationToken)
            .ConfigureAwait(false);

        return MapToDto(customerBasket, livePricing);
    }

    private static BasketDto MapToDto(ShoppingBasket customerBasket, BasketPricingResult livePricing)
    {
        var pricedLinesByProductId = livePricing.Lines.ToDictionary(pricedLine => pricedLine.ProductId);

        var itemDtos = customerBasket.Items
            .Select(basketItem => MapItem(basketItem, pricedLinesByProductId))
            .ToList();

        return new BasketDto(
            customerBasket.Id,
            customerBasket.CurrencyCode,
            itemDtos,
            RoundMoney(livePricing.Subtotal),
            RoundMoney(livePricing.DiscountTotal),
            RoundMoney(livePricing.TaxTotal),
            RoundMoney(livePricing.GrandTotal),
            livePricing.AppliedCoupon);
    }

    private static BasketItemDto MapItem(
        BasketItem basketItem,
        Dictionary<Guid, PricedBasketLine> pricedLinesByProductId)
    {
        if (!pricedLinesByProductId.TryGetValue(basketItem.ProductId, out var pricedLine))
        {
            throw new InternalServerException(
                $"Pricing response is missing a line for product \"{basketItem.ProductId}\".");
        }

        return new BasketItemDto(
            basketItem.ProductId,
            basketItem.ProductSku,
            basketItem.ProductName,
            basketItem.PrimaryCategory,
            basketItem.Quantity,
            RoundMoney(pricedLine.UnitPrice),
            RoundMoney(pricedLine.DiscountedUnitPrice),
            RoundMoney(pricedLine.LineTotal),
            basketItem.ImageUrl,
            basketItem.IsDigital);
    }

    private static decimal RoundMoney(decimal amount) =>
        Math.Round(amount, MoneyDecimalPlaces, MidpointRounding.ToEven);
}
