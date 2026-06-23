using FreshCart.Inventory.Api.Protos;
using FreshCart.Ordering.Application.Abstractions;
using FreshCart.Ordering.Infrastructure.Security;
using Grpc.Core;

namespace FreshCart.Ordering.Infrastructure.Inventory;

/// <summary>
/// Adapter from the <see cref="IInventoryClient"/> port to the generated Inventory gRPC stub. A
/// reservation rejection is a business result mapped onto <see cref="StockReservationResult"/>;
/// transport faults surface as gRPC exceptions so the caller's retry policy applies. Every call carries
/// the service-to-service bearer token the Inventory gRPC endpoint's <c>ServiceCaller</c> policy requires.
/// </summary>
public sealed class GrpcInventoryClient(
    InventoryService.InventoryServiceClient inventoryServiceClient,
    IServiceTokenProvider serviceTokenProvider) : IInventoryClient
{
    private const string AuthorizationHeaderName = "Authorization";

    public async Task<StockReservationResult> ReserveStockAsync(
        Guid orderId,
        IReadOnlyList<StockReservationLine> lines,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var request = new ReserveStockRequest { OrderId = orderId.ToString() };
        request.Lines.AddRange(lines.Select(line => new ReservationLine
        {
            ProductSku = line.ProductSku,
            Quantity = line.Quantity,
        }));

        var callOptions = await CreateAuthenticatedCallOptionsAsync(cancellationToken).ConfigureAwait(false);
        var response = await inventoryServiceClient
            .ReserveStockAsync(request, callOptions)
            .ConfigureAwait(false);

        if (response.Succeeded && Guid.TryParse(response.ReservationId, out var reservationId))
        {
            return StockReservationResult.Success(reservationId);
        }

        var reason = string.IsNullOrEmpty(response.FailureReason)
            ? "Inventory could not reserve the requested stock."
            : response.FailureReason;

        return StockReservationResult.Failure(reason, [.. response.UnavailableSkus]);
    }

    public async Task ReleaseReservationAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var request = new ReleaseReservationRequest { OrderId = orderId.ToString() };

        var callOptions = await CreateAuthenticatedCallOptionsAsync(cancellationToken).ConfigureAwait(false);
        await inventoryServiceClient
            .ReleaseReservationAsync(request, callOptions)
            .ConfigureAwait(false);
    }

    private async Task<CallOptions> CreateAuthenticatedCallOptionsAsync(CancellationToken cancellationToken)
    {
        var token = await serviceTokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        var metadata = new Metadata { { AuthorizationHeaderName, $"Bearer {token}" } };
        return new CallOptions(headers: metadata, cancellationToken: cancellationToken);
    }
}
