using System.Security.Claims;
using Carter;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Identity.Application.Account.Queries.GetCurrentUser;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FreshCart.Identity.Api.Endpoints.Account;

/// <summary>
/// Account-management endpoints. Currently exposes the <c>me</c> query used by the Angular shell to
/// hydrate session state on bootstrap. Password-reset and MFA endpoints land here in a later phase.
/// </summary>
public sealed class AccountEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var accountGroup = app.MapGroup("/account").WithTags("Account");

        accountGroup.MapGet("/me", GetCurrentUserAsync)
            .RequireAuthorization()
            .WithSummary("Return the authenticated user's profile.")
            .Produces<CurrentUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> GetCurrentUserAsync(
        ISender mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var subject = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub")
            ?? throw new ForbiddenException("Cannot identify the current user.");

        if (!Guid.TryParse(subject, out var userId))
        {
            throw new ForbiddenException("Authenticated subject is not a valid user identifier.");
        }

        var profile = await mediator
            .Send(new GetCurrentUserQuery(userId), cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(profile.Adapt<CurrentUserResponse>());
    }
}
