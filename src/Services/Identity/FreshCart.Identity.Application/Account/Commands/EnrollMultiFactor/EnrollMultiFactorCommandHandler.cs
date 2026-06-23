using System.Globalization;
using System.Text;
using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Identity.Application.Common.Abstractions;
using FreshCart.Identity.Domain.AuditEvents;
using FreshCart.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace FreshCart.Identity.Application.Account.Commands.EnrollMultiFactor;

public sealed class EnrollMultiFactorCommandHandler(
    UserManager<ApplicationUser> userManager,
    IIdentityAuditLog auditLog,
    ICurrentRequestContext currentRequest,
    ILogger<EnrollMultiFactorCommandHandler> logger)
    : ICommandHandler<EnrollMultiFactorCommand, EnrollMultiFactorResult>
{
    private const string AuthenticatorIssuer = "FreshCart";

#pragma warning disable S1075 // otpauth is the RFC 6238 key-URI scheme; the template is a protocol constant, not an environment-dependent address
    private static readonly CompositeFormat AuthenticatorUriFormat =
        CompositeFormat.Parse("otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6");
#pragma warning restore S1075

    private const int SharedKeyGroupLength = 4;

    public async Task<EnrollMultiFactorResult> Handle(
        EnrollMultiFactorCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await userManager.FindByIdAsync(command.UserId.ToString()).ConfigureAwait(false)
            ?? throw new NotFoundException("ApplicationUser", command.UserId);

        if (await userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false))
        {
            throw new BadRequestException(
                "Multi-factor authentication is already enabled. Disable it before enrolling a new authenticator.");
        }

        var resetResult = await userManager.ResetAuthenticatorKeyAsync(user).ConfigureAwait(false);
        if (!resetResult.Succeeded)
        {
            throw new InternalServerException("The authenticator key could not be reset.");
        }

        var authenticatorKey = await userManager.GetAuthenticatorKeyAsync(user).ConfigureAwait(false);
        if (string.IsNullOrEmpty(authenticatorKey))
        {
            throw new InternalServerException("The authenticator key is unavailable after reset.");
        }

        await RecordEnrollmentStartedAuditEventAsync(user, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("User {UserId} started multi-factor enrollment.", user.Id);

        return new EnrollMultiFactorResult(
            FormatSharedKey(authenticatorKey),
            BuildAuthenticatorUri(user.Email ?? string.Empty, authenticatorKey));
    }

    private static string FormatSharedKey(string authenticatorKey)
    {
        var groupCount = (authenticatorKey.Length + SharedKeyGroupLength - 1) / SharedKeyGroupLength;
        var formattedKey = new StringBuilder(authenticatorKey.Length + groupCount);

        for (var position = 0; position < authenticatorKey.Length; position += SharedKeyGroupLength)
        {
            if (position > 0)
            {
                formattedKey.Append(' ');
            }

            formattedKey.Append(
                authenticatorKey,
                position,
                Math.Min(SharedKeyGroupLength, authenticatorKey.Length - position));
        }

        return formattedKey.ToString();
    }

    private static Uri BuildAuthenticatorUri(string email, string authenticatorKey) =>
        new(string.Format(
            CultureInfo.InvariantCulture,
            AuthenticatorUriFormat,
            AuthenticatorIssuer,
            Uri.EscapeDataString(email),
            authenticatorKey));

    private Task RecordEnrollmentStartedAuditEventAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        auditLog.RecordAsync(
            new AuditEvent
            {
                EventType = AuditEventType.MultiFactorEnrollmentStarted,
                UserId = user.Id,
                Description = $"User {user.Email} started multi-factor enrollment; authenticator key reset.",
                IpAddress = currentRequest.IpAddress,
                UserAgent = currentRequest.UserAgent,
                CorrelationId = currentRequest.CorrelationId,
            },
            cancellationToken);
}
