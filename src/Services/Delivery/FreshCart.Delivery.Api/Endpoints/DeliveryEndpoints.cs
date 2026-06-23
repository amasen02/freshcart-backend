using System.Security.Claims;
using Carter;
using FreshCart.Delivery.Api.Configuration;
using FreshCart.Delivery.Application.Fulfilment;
using FreshCart.Delivery.Application.Tracking;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FreshCart.Delivery.Api.Endpoints;

/// <summary>
/// Carter module for the delivery bounded context. Slot discovery and order tracking are customer-facing
/// reads; the fulfilment transitions are back-office writes. Ownership for the tracking read is enforced
/// inside <see cref="DeliveryTrackingQueries"/> so the rule cannot be bypassed by a future caller.
/// </summary>
public sealed class DeliveryEndpoints : ICarterModule
{
    private const string CustomerPolicy = "Customer";
    private const string BackOfficeUserPolicy = "BackOfficeUser";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var deliveryGroup = app.MapGroup("/delivery").WithTags("Delivery");

        deliveryGroup.MapGet("/slots", GetOpenSlotsAsync)
            .RequireAuthorization(CustomerPolicy)
            .WithSummary("List delivery slots with free capacity on a given UTC date.")
            .Produces<IReadOnlyList<OpenSlotDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        deliveryGroup.MapGet("/orders/{orderId:guid}", GetTrackingAsync)
            .RequireAuthorization()
            .WithSummary("Track the delivery for an order (owning customer or Administrator).")
            .Produces<DeliveryTrackingDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        deliveryGroup.MapPost("/{deliveryId:guid}/out-for-delivery", StartOutForDeliveryAsync)
            .RequireAuthorization(BackOfficeUserPolicy)
            .WithSummary("Mark a scheduled delivery as out for delivery.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        deliveryGroup.MapPost("/{deliveryId:guid}/complete", CompleteAsync)
            .RequireAuthorization(BackOfficeUserPolicy)
            .WithSummary("Complete a delivery that is out for delivery and publish DeliveryCompleted.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> GetOpenSlotsAsync(
        DateOnly date,
        DeliveryTrackingQueries trackingQueries,
        CancellationToken cancellationToken)
    {
        var openSlots = await trackingQueries.ListOpenSlotsAsync(date, cancellationToken).ConfigureAwait(false);
        return Results.Ok(openSlots);
    }

    private static async Task<IResult> GetTrackingAsync(
        Guid orderId,
        ClaimsPrincipal principal,
        DeliveryTrackingQueries trackingQueries,
        CancellationToken cancellationToken)
    {
        var tracking = await trackingQueries
            .GetTrackingForOrderAsync(
                orderId,
                principal.GetRequiredCustomerId(),
                principal.IsAdministrator(),
                cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(tracking);
    }

    private static async Task<IResult> StartOutForDeliveryAsync(
        Guid deliveryId,
        CompleteDeliveryService completeDeliveryService,
        CancellationToken cancellationToken)
    {
        await completeDeliveryService.StartOutForDeliveryAsync(deliveryId, cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }

    private static async Task<IResult> CompleteAsync(
        Guid deliveryId,
        CompleteDeliveryService completeDeliveryService,
        CancellationToken cancellationToken)
    {
        await completeDeliveryService.CompleteAsync(deliveryId, cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }
}
