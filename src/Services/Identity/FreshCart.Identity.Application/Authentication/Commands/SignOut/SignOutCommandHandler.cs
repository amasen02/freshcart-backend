using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Identity.Application.Common.Abstractions;
using FreshCart.Identity.Domain.AuditEvents;
using FreshCart.Identity.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace FreshCart.Identity.Application.Authentication.Commands.SignOut;

public sealed class SignOutCommandHandler(
    UserManager<ApplicationUser> userManager,
    IRefreshTokenService refreshTokenService,
    IIdentityAuditLog auditLog,
    ICurrentRequestContext currentRequest)
    : ICommandHandler<SignOutCommand, Unit>
{
    public async Task<Unit> Handle(SignOutCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await userManager.FindByIdAsync(command.UserId.ToString()).ConfigureAwait(false)
            ?? throw new NotFoundException("ApplicationUser", command.UserId);

        await refreshTokenService
            .RevokeAllForUserAsync(user.Id, "User signed out.", cancellationToken)
            .ConfigureAwait(false);

        user.InvalidateExistingSessions(DateTimeOffset.UtcNow);
        await userManager.UpdateSecurityStampAsync(user).ConfigureAwait(false);
        await userManager.UpdateAsync(user).ConfigureAwait(false);

        await auditLog.RecordAsync(
            new AuditEvent
            {
                EventType = AuditEventType.SignedOut,
                UserId = user.Id,
                Description = "User signed out; refresh tokens revoked.",
                IpAddress = currentRequest.IpAddress,
                UserAgent = currentRequest.UserAgent,
                CorrelationId = currentRequest.CorrelationId,
            },
            cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
