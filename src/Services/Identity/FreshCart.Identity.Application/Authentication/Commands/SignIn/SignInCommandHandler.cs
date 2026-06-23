using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Identity.Application.Common.Abstractions;
using FreshCart.Identity.Application.Common.Models;
using FreshCart.Identity.Domain.AuditEvents;
using FreshCart.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace FreshCart.Identity.Application.Authentication.Commands.SignIn;

public sealed class SignInCommandHandler(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IAccessTokenIssuer accessTokenIssuer,
    IRefreshTokenService refreshTokenService,
    IIdentityAuditLog auditLog,
    ICurrentRequestContext currentRequest,
    ILogger<SignInCommandHandler> logger)
    : ICommandHandler<SignInCommand, SignInResult>
{
    // Identical message for unknown-email and bad-password paths so an attacker cannot
    // enumerate accounts by comparing responses.
    private const string CredentialMismatchMessage = "Email or password is incorrect.";

    public async Task<SignInResult> Handle(SignInCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await userManager.FindByEmailAsync(command.Email).ConfigureAwait(false);
        if (user is null)
        {
            await AuditFailureAsync(null, "Unknown email.", cancellationToken).ConfigureAwait(false);
            throw new BadRequestException(CredentialMismatchMessage);
        }

        var passwordCheck = await signInManager
            .CheckPasswordSignInAsync(user, command.Password, lockoutOnFailure: true)
            .ConfigureAwait(false);

        if (passwordCheck.IsLockedOut)
        {
            await AuditLockoutAsync(user.Id, cancellationToken).ConfigureAwait(false);
            throw new ForbiddenException("Account is temporarily locked due to repeated failed sign-in attempts.");
        }

        if (!passwordCheck.Succeeded)
        {
            await AuditFailureAsync(user.Id, "Bad password.", cancellationToken).ConfigureAwait(false);
            throw new BadRequestException(CredentialMismatchMessage);
        }

        if (await userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false))
        {
            await VerifyMultiFactorOrThrowAsync(user, command.MultiFactorCode, cancellationToken).ConfigureAwait(false);
        }

        user.RecordSuccessfulSignIn(DateTimeOffset.UtcNow);
        await userManager.UpdateAsync(user).ConfigureAwait(false);

        var assignedRoles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        var roleNames = assignedRoles.ToArray();
        var profile = new AuthenticationProfile(
            UserId: user.Id,
            Email: user.Email ?? string.Empty,
            DisplayName: user.DisplayName,
            Roles: roleNames,
            MultiFactorEnabled: await userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false));

        await AuditSuccessAsync(user, command.UseCookie, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("User {UserId} signed in.", user.Id);

        if (command.UseCookie)
        {
            return new SignInResult(profile, null, null, null);
        }

        var accessToken = accessTokenIssuer.Issue(user, roleNames);
        var refreshToken = await refreshTokenService
            .IssueAsync(user.Id, currentRequest.IpAddress, currentRequest.UserAgent, cancellationToken)
            .ConfigureAwait(false);

        return new SignInResult(profile, accessToken.AccessToken, refreshToken.PlaintextToken, accessToken.ExpiresOnUtc);
    }

    private async Task VerifyMultiFactorOrThrowAsync(
        ApplicationUser user,
        string? multiFactorCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(multiFactorCode))
        {
            throw new BadRequestException("Multi-factor code is required.");
        }

        var verified = await userManager
            .VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, multiFactorCode)
            .ConfigureAwait(false);

        if (verified)
        {
            return;
        }

        await AuditFailureAsync(user.Id, "Bad MFA code.", cancellationToken).ConfigureAwait(false);
        throw new BadRequestException("Multi-factor code is incorrect.");
    }

    private Task AuditSuccessAsync(ApplicationUser user, bool isCookieMode, CancellationToken cancellationToken) =>
        auditLog.RecordAsync(
            new AuditEvent
            {
                EventType = AuditEventType.SignInSucceeded,
                UserId = user.Id,
                Description = $"User {user.Email} signed in (cookie={isCookieMode}).",
                IpAddress = currentRequest.IpAddress,
                UserAgent = currentRequest.UserAgent,
                CorrelationId = currentRequest.CorrelationId,
            },
            cancellationToken);

    private Task AuditFailureAsync(Guid? userId, string description, CancellationToken cancellationToken) =>
        auditLog.RecordAsync(
            new AuditEvent
            {
                EventType = AuditEventType.SignInFailed,
                UserId = userId,
                Description = description,
                IpAddress = currentRequest.IpAddress,
                UserAgent = currentRequest.UserAgent,
                CorrelationId = currentRequest.CorrelationId,
            },
            cancellationToken);

    private Task AuditLockoutAsync(Guid userId, CancellationToken cancellationToken) =>
        auditLog.RecordAsync(
            new AuditEvent
            {
                EventType = AuditEventType.AccountLockedOut,
                UserId = userId,
                Description = "Account locked due to failed sign-in attempts.",
                IpAddress = currentRequest.IpAddress,
                UserAgent = currentRequest.UserAgent,
                CorrelationId = currentRequest.CorrelationId,
            },
            cancellationToken);
}
