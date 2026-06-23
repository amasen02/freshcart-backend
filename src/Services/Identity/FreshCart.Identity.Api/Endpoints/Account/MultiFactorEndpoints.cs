using System.Security.Claims;
using Carter;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Identity.Api.Configuration;
using FreshCart.Identity.Application.Account.Commands.DisableMultiFactor;
using FreshCart.Identity.Application.Account.Commands.EnrollMultiFactor;
using FreshCart.Identity.Application.Account.Commands.VerifyMultiFactorEnrollment;
using MediatR;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FreshCart.Identity.Api.Endpoints.Account;

/// <summary>
/// Carter module for the authenticator (TOTP) lifecycle: enroll, verify enrollment, disable. The user
/// identity always comes from the authenticated principal, never from the request body, so one user
/// cannot manage another user's authenticator.
/// </summary>
public sealed class MultiFactorEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var multiFactorGroup = app
            .MapGroup("/account/mfa")
            .WithTags("Account")
            .RequireAuthorization();

        // Every route here is an authenticated state change, so enforce the double-submit CSRF token for
        // browser (cookie) callers; bearer / service callers carry no cookie and are skipped.
        multiFactorGroup.AddEndpointFilter(static async (filterContext, next) =>
        {
            var antiforgery = filterContext.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
            await AntiforgeryConfiguration.ValidateBrowserRequestAsync(filterContext.HttpContext, antiforgery)
                .ConfigureAwait(false);
            return await next(filterContext).ConfigureAwait(false);
        });

        multiFactorGroup.MapPost("/enroll", EnrollAsync)
            .WithSummary("Start authenticator enrollment and return the shared key plus otpauth URI.")
            .Produces<EnrollMultiFactorResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        multiFactorGroup.MapPost("/verify", VerifyAsync)
            .WithSummary("Confirm enrollment with a TOTP code and receive the one-time recovery codes.")
            .Produces<VerifyMultiFactorEnrollmentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        multiFactorGroup.MapPost("/disable", DisableAsync)
            .WithSummary("Disable multi-factor authentication after verifying a current TOTP code.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> EnrollAsync(
        ISender mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var authenticatedUserId = ResolveAuthenticatedUserId(httpContext);

        var commandResult = await mediator
            .Send(new EnrollMultiFactorCommand(authenticatedUserId), cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(new EnrollMultiFactorResponse(commandResult.SharedKey, commandResult.AuthenticatorUri));
    }

    private static async Task<IResult> VerifyAsync(
        VerifyMultiFactorEnrollmentRequest verifyRequest,
        ISender mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var authenticatedUserId = ResolveAuthenticatedUserId(httpContext);

        var commandResult = await mediator
            .Send(
                new VerifyMultiFactorEnrollmentCommand(authenticatedUserId, verifyRequest.VerificationCode),
                cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(new VerifyMultiFactorEnrollmentResponse(commandResult.RecoveryCodes));
    }

    private static async Task<IResult> DisableAsync(
        DisableMultiFactorRequest disableRequest,
        ISender mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var authenticatedUserId = ResolveAuthenticatedUserId(httpContext);

        await mediator
            .Send(new DisableMultiFactorCommand(authenticatedUserId, disableRequest.VerificationCode), cancellationToken)
            .ConfigureAwait(false);

        return Results.NoContent();
    }

    private static Guid ResolveAuthenticatedUserId(HttpContext httpContext)
    {
        var subject = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub")
            ?? throw new ForbiddenException("Cannot identify the current user.");

        if (!Guid.TryParse(subject, out var authenticatedUserId))
        {
            throw new ForbiddenException("Authenticated subject is not a valid user identifier.");
        }

        return authenticatedUserId;
    }
}
