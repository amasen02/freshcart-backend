using System.Security.Claims;
using Carter;
using FreshCart.Basket.Api.Authentication;
using MediatR;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.UpdateItemQuantity;

public sealed class UpdateItemQuantityEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPut(BasketEndpointConventions.ItemRoute, UpdateItemQuantityAsync)
            .RequireAuthorization(AuthorizationPolicies.Customer)
            .WithTags(BasketEndpointConventions.Tag)
            .WithSummary("Set the quantity of a basket line; quantity 0 removes the line.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> UpdateItemQuantityAsync(
        Guid productId,
        UpdateItemQuantityRequest updateItemQuantityRequest,
        ClaimsPrincipal claimsPrincipal,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(updateItemQuantityRequest);

        var command = new UpdateItemQuantityCommand(
            claimsPrincipal.GetCustomerId(),
            productId,
            updateItemQuantityRequest.Quantity);

        await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }
}
