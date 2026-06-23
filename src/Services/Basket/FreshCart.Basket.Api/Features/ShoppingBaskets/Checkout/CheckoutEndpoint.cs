using System.Security.Claims;
using Carter;
using FreshCart.Basket.Api.Authentication;
using MediatR;

namespace FreshCart.Basket.Api.Features.ShoppingBaskets.Checkout;

public sealed class CheckoutEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost(BasketEndpointConventions.CheckoutRoute, CheckoutAsync)
            .RequireAuthorization(AuthorizationPolicies.Customer)
            .WithTags(BasketEndpointConventions.Tag)
            .WithSummary("Start checkout: reprice, archive the basket and enqueue the order request. The order itself is created asynchronously, hence 202.")
            .Produces<CheckoutResult>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> CheckoutAsync(
        CheckoutRequest checkoutRequest,
        ClaimsPrincipal claimsPrincipal,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(checkoutRequest);

        var command = new CheckoutCommand(
            claimsPrincipal.GetCustomerId(),
            claimsPrincipal.GetCustomerEmail(),
            claimsPrincipal.GetCustomerDisplayName(),
            checkoutRequest.PaymentMethod,
            checkoutRequest.BillingAddress,
            checkoutRequest.ShippingAddress);

        var checkoutResult = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Results.Accepted(value: checkoutResult);
    }
}
