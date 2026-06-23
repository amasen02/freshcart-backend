using System.Security.Claims;
using Carter;
using FreshCart.Basket.Api.Authentication;
using MediatR;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.RemoveItem;

public sealed class RemoveItemEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapDelete(BasketEndpointConventions.ItemRoute, RemoveItemAsync)
            .RequireAuthorization(AuthorizationPolicies.Customer)
            .WithTags(BasketEndpointConventions.Tag)
            .WithSummary("Remove a product line from the basket.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> RemoveItemAsync(
        Guid productId,
        ClaimsPrincipal claimsPrincipal,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        var command = new RemoveItemCommand(claimsPrincipal.GetCustomerId(), productId);
        await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }
}
