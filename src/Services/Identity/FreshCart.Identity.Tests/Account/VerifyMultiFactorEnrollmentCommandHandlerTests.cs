using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Identity.Application.Account.Commands.VerifyMultiFactorEnrollment;
using FreshCart.Identity.Application.Common.Abstractions;
using FreshCart.Identity.Domain.AuditEvents;
using FreshCart.Identity.Domain.Users;
using FreshCart.Identity.Tests.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FreshCart.Identity.Tests.Account;

public sealed class VerifyMultiFactorEnrollmentCommandHandlerTests : IDisposable
{
    private const string ValidVerificationCode = "123456";

    private readonly UserManager<ApplicationUser> _userManager = UserManagerSubstitute.Create();
    private readonly IIdentityAuditLog _auditLog = Substitute.For<IIdentityAuditLog>();
    private readonly ICurrentRequestContext _currentRequest = Substitute.For<ICurrentRequestContext>();
    private readonly VerifyMultiFactorEnrollmentCommandHandler _handler;
    private readonly ApplicationUser _user;

    public VerifyMultiFactorEnrollmentCommandHandlerTests()
    {
        _user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "customer@freshcart.test",
            DisplayName = "Test Customer",
        };

        _handler = new VerifyMultiFactorEnrollmentCommandHandler(
            _userManager,
            _auditLog,
            _currentRequest,
            NullLogger<VerifyMultiFactorEnrollmentCommandHandler>.Instance);
    }

    [Fact]
    public async Task VerifyEnablesMultiFactorAndReturnsTheEightRecoveryCodes()
    {
        var recoveryCodes = new[] { "r1", "r2", "r3", "r4", "r5", "r6", "r7", "r8" };
        _userManager.FindByIdAsync(_user.Id.ToString()).Returns(_user);
        _userManager.GetTwoFactorEnabledAsync(_user).Returns(false);
        _userManager
            .VerifyTwoFactorTokenAsync(_user, TokenOptions.DefaultAuthenticatorProvider, ValidVerificationCode)
            .Returns(true);
        _userManager.SetTwoFactorEnabledAsync(_user, true).Returns(IdentityResult.Success);
        _userManager.GenerateNewTwoFactorRecoveryCodesAsync(_user, 8).Returns(recoveryCodes);

        var result = await _handler
            .Handle(new VerifyMultiFactorEnrollmentCommand(_user.Id, ValidVerificationCode), CancellationToken.None);

        result.RecoveryCodes.Should().Equal(recoveryCodes);
        await _userManager.Received(1).SetTwoFactorEnabledAsync(_user, true);
        await _userManager.Received(1).GenerateNewTwoFactorRecoveryCodesAsync(_user, 8);
        await _userManager.Received(1).UpdateAsync(_user);
        await _auditLog.Received(1).RecordAsync(
            Arg.Is<AuditEvent>(auditEvent =>
                auditEvent.EventType == AuditEventType.MultiFactorEnabled && auditEvent.UserId == _user.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyThrowsBadRequestAndAuditsWhenCodeIsIncorrect()
    {
        _userManager.FindByIdAsync(_user.Id.ToString()).Returns(_user);
        _userManager.GetTwoFactorEnabledAsync(_user).Returns(false);
        _userManager
            .VerifyTwoFactorTokenAsync(_user, TokenOptions.DefaultAuthenticatorProvider, ValidVerificationCode)
            .Returns(false);

        var verifying = () => _handler.Handle(
            new VerifyMultiFactorEnrollmentCommand(_user.Id, ValidVerificationCode),
            CancellationToken.None);

        await verifying.Should().ThrowAsync<BadRequestException>();
        await _userManager.DidNotReceive()
            .SetTwoFactorEnabledAsync(Arg.Any<ApplicationUser>(), Arg.Any<bool>());
        await _auditLog.Received(1).RecordAsync(
            Arg.Is<AuditEvent>(auditEvent =>
                auditEvent.EventType == AuditEventType.MultiFactorVerificationFailed
                && auditEvent.UserId == _user.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyThrowsBadRequestWhenMultiFactorIsAlreadyEnabled()
    {
        _userManager.FindByIdAsync(_user.Id.ToString()).Returns(_user);
        _userManager.GetTwoFactorEnabledAsync(_user).Returns(true);

        var verifying = () => _handler.Handle(
            new VerifyMultiFactorEnrollmentCommand(_user.Id, ValidVerificationCode),
            CancellationToken.None);

        await verifying.Should().ThrowAsync<BadRequestException>();
        await _userManager.DidNotReceive().VerifyTwoFactorTokenAsync(
            Arg.Any<ApplicationUser>(),
            Arg.Any<string>(),
            Arg.Any<string>());
        await _userManager.DidNotReceive().GenerateNewTwoFactorRecoveryCodesAsync(
            Arg.Any<ApplicationUser>(),
            Arg.Any<int>());
    }

    [Fact]
    public Task VerifyThrowsNotFoundWhenUserDoesNotExist()
    {
        _userManager.FindByIdAsync(Arg.Any<string>()).Returns((ApplicationUser?)null);

        var verifying = () => _handler.Handle(
            new VerifyMultiFactorEnrollmentCommand(Guid.NewGuid(), ValidVerificationCode),
            CancellationToken.None);

        return verifying.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task VerifyThrowsInternalServerWhenEnablingTwoFactorFails()
    {
        _userManager.FindByIdAsync(_user.Id.ToString()).Returns(_user);
        _userManager.GetTwoFactorEnabledAsync(_user).Returns(false);
        _userManager
            .VerifyTwoFactorTokenAsync(_user, TokenOptions.DefaultAuthenticatorProvider, ValidVerificationCode)
            .Returns(true);
        _userManager.SetTwoFactorEnabledAsync(_user, true).Returns(IdentityResult.Failed());

        var verifying = () => _handler.Handle(
            new VerifyMultiFactorEnrollmentCommand(_user.Id, ValidVerificationCode),
            CancellationToken.None);

        await verifying.Should().ThrowAsync<InternalServerException>();
        await _userManager.DidNotReceive().GenerateNewTwoFactorRecoveryCodesAsync(
            Arg.Any<ApplicationUser>(),
            Arg.Any<int>());
    }

    [Fact]
    public Task VerifyThrowsInternalServerWhenRecoveryCodesCannotBeGenerated()
    {
        _userManager.FindByIdAsync(_user.Id.ToString()).Returns(_user);
        _userManager.GetTwoFactorEnabledAsync(_user).Returns(false);
        _userManager
            .VerifyTwoFactorTokenAsync(_user, TokenOptions.DefaultAuthenticatorProvider, ValidVerificationCode)
            .Returns(true);
        _userManager.SetTwoFactorEnabledAsync(_user, true).Returns(IdentityResult.Success);
        _userManager.GenerateNewTwoFactorRecoveryCodesAsync(_user, 8).Returns((IEnumerable<string>?)null);

        var verifying = () => _handler.Handle(
            new VerifyMultiFactorEnrollmentCommand(_user.Id, ValidVerificationCode),
            CancellationToken.None);

        return verifying.Should().ThrowAsync<InternalServerException>();
    }

    public void Dispose() => _userManager.Dispose();
}
