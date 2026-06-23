using System.Security.Claims;
using Carter;
using FreshCart.Basket.Api.Authentication;
using MediatR;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.AddItem;

public sealed class AddItemEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost(BasketEndpointConventions.ItemsRoute, AddItemAsync)
            .RequireAuthorization(AuthorizationPolicies.Customer)
            .WithTags(BasketEndpointConventions.Tag)
            .WithSummary("Add a product to the basket, merging quantities when the line already exists.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> AddItemAsync(
        AddItemRequest addItemRequest,
        ClaimsPrincipal claimsPrincipal,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(addItemRequest);

        var command = new AddItemCommand(claimsPrincipal.GetCustomerId(), addItemRequest.ProductId, addItemRequest.Quantity);
        await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }
}
