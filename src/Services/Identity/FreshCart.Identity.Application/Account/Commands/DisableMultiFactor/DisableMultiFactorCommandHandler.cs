using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Identity.Application.Common.Abstractions;
using FreshCart.Identity.Domain.AuditEvents;
using FreshCart.Identity.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace FreshCart.Identity.Application.Account.Commands.DisableMultiFactor;

public sealed class DisableMultiFactorCommandHandler(
    UserManager<ApplicationUser> userManager,
    IIdentityAuditLog auditLog,
    ICurrentRequestContext currentRequest,
    ILogger<DisableMultiFactorCommandHandler> logger)
    : ICommandHandler<DisableMultiFactorCommand, Unit>
{
    public async Task<Unit> Handle(DisableMultiFactorCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await userManager.FindByIdAsync(command.UserId.ToString()).ConfigureAwait(false)
            ?? throw new NotFoundException("ApplicationUser", command.UserId);

        if (!await userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false))
        {
            throw new BadRequestException("Multi-factor authentication is not enabled.");
        }

        var codeIsValid = await userManager
            .VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, command.VerificationCode)
            .ConfigureAwait(false);

        if (!codeIsValid)
        {
            await RecordVerificationFailedAuditEventAsync(user, cancellationToken).ConfigureAwait(false);
            throw new BadRequestException("Verification code is incorrect.");
        }

        var disableResult = await userManager.SetTwoFactorEnabledAsync(user, false).ConfigureAwait(false);
        if (!disableResult.Succeeded)
        {
            throw new InternalServerException("Multi-factor authentication could not be disabled.");
        }

        user.InvalidateExistingSessions(DateTimeOffset.UtcNow);
        await userManager.UpdateAsync(user).ConfigureAwait(false);

        await RecordMultiFactorDisabledAuditEventAsync(user, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("User {UserId} disabled multi-factor authentication.", user.Id);

        return Unit.Value;
    }

    private Task RecordVerificationFailedAuditEventAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        auditLog.RecordAsync(
            new AuditEvent
            {
                EventType = AuditEventType.MultiFactorVerificationFailed,
                UserId = user.Id,
                Description = "Bad verification code while disabling multi-factor authentication.",
                IpAddress = currentRequest.IpAddress,
                UserAgent = currentRequest.UserAgent,
                CorrelationId = currentRequest.CorrelationId,
            },
            cancellationToken);

    private Task RecordMultiFactorDisabledAuditEventAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        auditLog.RecordAsync(
            new AuditEvent
            {
                EventType = AuditEventType.MultiFactorDisabled,
                UserId = user.Id,
                Description = $"User {user.Email} disabled multi-factor authentication.",
                IpAddress = currentRequest.IpAddress,
                UserAgent = currentRequest.UserAgent,
                CorrelationId = currentRequest.CorrelationId,
            },
            cancellationToken);
}
