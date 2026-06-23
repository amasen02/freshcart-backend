using FreshCart.BuildingBlocks.Security;
using FreshCart.Inventory.Api.Models;
using FreshCart.Inventory.Api.Protos;
using FreshCart.Inventory.Api.Services;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace FreshCart.Inventory.Api.Grpc;

[Authorize(Policy = ServiceAuthenticationDefaults.ServiceCallerPolicy)]
public sealed class InventoryGrpcService(IStockReservationService stockReservationService)
    : InventoryService.InventoryServiceBase
{
    public override async Task<ReserveStockResponse> ReserveStock(ReserveStockRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var orderId = ParseOrderId(request.OrderId);
        var reservationLines = MapReservationLines(request);

        var reservationResult = await stockReservationService
            .ReserveAsync(orderId, reservationLines, context.CancellationToken)
            .ConfigureAwait(false);

        var response = new ReserveStockResponse
        {
            Succeeded = reservationResult.Succeeded,
            ReservationId = reservationResult.Succeeded ? reservationResult.ReservationId.ToString() : string.Empty,
            FailureReason = reservationResult.FailureReason ?? string.Empty,
        };

        response.UnavailableSkus.AddRange(reservationResult.UnavailableSkus);

        return response;
    }

    public override async Task<ReleaseReservationResponse> ReleaseReservation(
        ReleaseReservationRequest request,
        ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var orderId = ParseOrderId(request.OrderId);

        var released = await stockReservationService
            .ReleaseAsync(orderId, context.CancellationToken)
            .ConfigureAwait(false);

        return new ReleaseReservationResponse { Released = released };
    }

    private static List<StockReservationLine> MapReservationLines(ReserveStockRequest request)
    {
        if (request.Lines.Count == 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "At least one reservation line is required."));
        }

        var reservationLines = new List<StockReservationLine>(request.Lines.Count);

        foreach (var requestLine in request.Lines)
        {
            if (string.IsNullOrWhiteSpace(requestLine.ProductSku))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Every reservation line requires a product sku."));
            }

            if (requestLine.Quantity <= 0)
            {
                throw new RpcException(new Status(
                    StatusCode.InvalidArgument,
                    $"Quantity for sku \"{requestLine.ProductSku}\" must be a positive number."));
            }

            reservationLines.Add(new StockReservationLine
            {
                ProductSku = requestLine.ProductSku,
                Quantity = requestLine.Quantity,
            });
        }

        return reservationLines;
    }

    private static Guid ParseOrderId(string orderId)
    {
        if (!Guid.TryParse(orderId, out var parsedOrderId) || parsedOrderId == Guid.Empty)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "order_id must be a non-empty GUID."));
        }

        return parsedOrderId;
    }
}
