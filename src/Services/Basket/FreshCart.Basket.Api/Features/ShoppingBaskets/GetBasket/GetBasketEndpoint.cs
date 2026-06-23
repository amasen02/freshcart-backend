using System.Security.Claims;
using Carter;
using FreshCart.Basket.Api.Authentication;
using MediatR;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.GetBasket;

public sealed class GetBasketEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet(BasketEndpointConventions.BasketRoute, GetBasketAsync)
            .RequireAuthorization(AuthorizationPolicies.Customer)
            .WithTags(BasketEndpointConventions.Tag)
            .WithSummary("Get the current customer's basket with live pricing. An empty basket returns an empty payload, not 404.")
            .Produces<BasketDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> GetBasketAsync(
        ClaimsPrincipal claimsPrincipal,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetBasketQuery(claimsPrincipal.GetCustomerId());
        var basketDto = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Results.Ok(basketDto);
    }
}
