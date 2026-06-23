using System.Security.Claims;
using Carter;
using FreshCart.Basket.Api.Authentication;
using MediatR;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.RemoveCoupon;

public sealed class RemoveCouponEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapDelete(BasketEndpointConventions.CouponRoute, RemoveCouponAsync)
            .RequireAuthorization(AuthorizationPolicies.Customer)
            .WithTags(BasketEndpointConventions.Tag)
            .WithSummary("Remove the applied coupon code from the basket.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> RemoveCouponAsync(
        ClaimsPrincipal claimsPrincipal,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        var command = new RemoveCouponCommand(claimsPrincipal.GetCustomerId());
        await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }
}
