using FreshCart.Pricing.Grpc.Protos;

namespace FreshCart.Basket.Api.Pricing;

/// <summary>
/// Adapter from the <see cref="IBasketPricingClient"/> port to the generated Pricing gRPC stub.
/// All decimal-to-wire conversion lives here.
/// </summary>
public sealed class GrpcBasketPricingClient(PricingService.PricingServiceClient pricingServiceClient) : IBasketPricingClient
{
    public async Task<BasketPricingResult> PriceBasketAsync(BasketPricingRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var grpcRequest = new PriceBasketRequest
        {
            CustomerId = request.CustomerId.ToString(),
            CouponCode = request.CouponCode ?? string.Empty,
            CurrencyCode = request.CurrencyCode,
        };

        grpcRequest.Lines.AddRange(request.Lines.Select(line => new PriceBasketLine
        {
            ProductId = line.ProductId.ToString(),
            ProductSku = line.ProductSku,
            UnitPrice = GrpcMoney.ToWireFormat(line.UnitPrice),
            Quantity = line.Quantity,
        }));

        var grpcResponse = await pricingServiceClient
            .PriceBasketAsync(grpcRequest, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return new BasketPricingResult(
            [.. grpcResponse.Lines.Select(pricedLine => new PricedBasketLine(
                Guid.Parse(pricedLine.ProductId),
                GrpcMoney.Parse(pricedLine.UnitPrice),
                GrpcMoney.Parse(pricedLine.DiscountedUnitPrice),
                GrpcMoney.Parse(pricedLine.LineTotal)))],
            GrpcMoney.Parse(grpcResponse.Subtotal),
            GrpcMoney.Parse(grpcResponse.DiscountTotal),
            GrpcMoney.Parse(grpcResponse.TaxTotal),
            GrpcMoney.Parse(grpcResponse.GrandTotal),
            string.IsNullOrEmpty(grpcResponse.AppliedCoupon) ? null : grpcResponse.AppliedCoupon);
    }

    public async Task<CouponValidationResult> ValidateCouponAsync(
        string couponCode,
        Guid customerId,
        decimal orderSubtotal,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(couponCode);

        var grpcRequest = new ValidateCouponRequest
        {
            CouponCode = couponCode,
            CustomerId = customerId.ToString(),
            OrderSubtotal = GrpcMoney.ToWireFormat(orderSubtotal),
        };

        var grpcResponse = await pricingServiceClient
            .ValidateCouponAsync(grpcRequest, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return new CouponValidationResult(
            grpcResponse.IsValid,
            string.IsNullOrEmpty(grpcResponse.ErrorMessage) ? null : grpcResponse.ErrorMessage,
            string.IsNullOrEmpty(grpcResponse.DiscountValue) ? decimal.Zero : GrpcMoney.Parse(grpcResponse.DiscountValue),
            string.IsNullOrEmpty(grpcResponse.DiscountType) ? null : grpcResponse.DiscountType);
    }
}
