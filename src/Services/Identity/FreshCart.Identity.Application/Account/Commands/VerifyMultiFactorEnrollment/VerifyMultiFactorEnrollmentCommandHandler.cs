using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Identity.Application.Common.Abstractions;
using FreshCart.Identity.Domain.AuditEvents;
using FreshCart.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace FreshCart.Identity.Application.Account.Commands.VerifyMultiFactorEnrollment;

public sealed class VerifyMultiFactorEnrollmentCommandHandler(
    UserManager<ApplicationUser> userManager,
    IIdentityAuditLog auditLog,
    ICurrentRequestContext currentRequest,
    ILogger<VerifyMultiFactorEnrollmentCommandHandler> logger)
    : ICommandHandler<VerifyMultiFactorEnrollmentCommand, VerifyMultiFactorEnrollmentResult>
{
    private const int RecoveryCodeCount = 8;

    public async Task<VerifyMultiFactorEnrollmentResult> Handle(
        VerifyMultiFactorEnrollmentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await userManager.FindByIdAsync(command.UserId.ToString()).ConfigureAwait(false)
            ?? throw new NotFoundException("ApplicationUser", command.UserId);

        if (await userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false))
        {
            throw new BadRequestException("Multi-factor authentication is already enabled.");
        }

        var codeIsValid = await userManager
            .VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, command.VerificationCode)
            .ConfigureAwait(false);

        if (!codeIsValid)
        {
            await RecordVerificationFailedAuditEventAsync(user, cancellationToken).ConfigureAwait(false);
            throw new BadRequestException("Verification code is incorrect.");
        }

        var enableResult = await userManager.SetTwoFactorEnabledAsync(user, true).ConfigureAwait(false);
        if (!enableResult.Succeeded)
        {
            throw new InternalServerException("Multi-factor authentication could not be enabled.");
        }

        user.InvalidateExistingSessions(DateTimeOffset.UtcNow);
        await userManager.UpdateAsync(user).ConfigureAwait(false);

        var recoveryCodes = await userManager
            .GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodeCount)
            .ConfigureAwait(false)
            ?? throw new InternalServerException("Recovery codes could not be generated.");

        await RecordMultiFactorEnabledAuditEventAsync(user, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("User {UserId} enabled multi-factor authentication.", user.Id);

        return new VerifyMultiFactorEnrollmentResult(recoveryCodes.ToArray());
    }

    private Task RecordVerificationFailedAuditEventAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        auditLog.RecordAsync(
            new AuditEvent
            {
                EventType = AuditEventType.MultiFactorVerificationFailed,
                UserId = user.Id,
                Description = "Bad verification code during multi-factor enrollment.",
                IpAddress = currentRequest.IpAddress,
                UserAgent = currentRequest.UserAgent,
                CorrelationId = currentRequest.CorrelationId,
            },
            cancellationToken);

    private Task RecordMultiFactorEnabledAuditEventAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        auditLog.RecordAsync(
            new AuditEvent
            {
                EventType = AuditEventType.MultiFactorEnabled,
                UserId = user.Id,
                Description = $"User {user.Email} enabled multi-factor authentication; recovery codes regenerated.",
                IpAddress = currentRequest.IpAddress,
                UserAgent = currentRequest.UserAgent,
                CorrelationId = currentRequest.CorrelationId,
            },
            cancellationToken);
}
