using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Identity.Application.Common.Abstractions;
using FreshCart.Identity.Domain.AuditEvents;
using FreshCart.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace FreshCart.Identity.Application.Authentication.Commands.RefreshAccessToken;

public sealed class RefreshAccessTokenCommandHandler(
    UserManager<ApplicationUser> userManager,
    IAccessTokenIssuer accessTokenIssuer,
    IRefreshTokenService refreshTokenService,
    IIdentityAuditLog auditLog,
    ICurrentRequestContext currentRequest)
    : ICommandHandler<RefreshAccessTokenCommand, RefreshAccessTokenResult>
{
    public async Task<RefreshAccessTokenResult> Handle(
        RefreshAccessTokenCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            throw new BadRequestException("Refresh token is required.");
        }

        var rotation = await refreshTokenService
            .RotateAsync(
                command.RefreshToken,
                currentRequest.IpAddress,
                currentRequest.UserAgent,
                cancellationToken)
            .ConfigureAwait(false);

        var user = await userManager.FindByIdAsync(rotation.UserId.ToString()).ConfigureAwait(false)
            ?? throw new NotFoundException("ApplicationUser", rotation.UserId);

        var assignedRoles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        var accessTokenResult = accessTokenIssuer.Issue(user, assignedRoles.ToArray());

        await auditLog.RecordAsync(
            new AuditEvent
            {
                EventType = AuditEventType.RefreshTokenIssued,
                UserId = user.Id,
                Description = "Refresh token rotated.",
                IpAddress = currentRequest.IpAddress,
                UserAgent = currentRequest.UserAgent,
                CorrelationId = currentRequest.CorrelationId,
            },
            cancellationToken).ConfigureAwait(false);

        return new RefreshAccessTokenResult(
            AccessToken: accessTokenResult.AccessToken,
            RefreshToken: rotation.NewPlaintextToken,
            AccessTokenExpiresOnUtc: accessTokenResult.ExpiresOnUtc);
    }
}
