using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Identity.Application.Account.Commands.DisableMultiFactor;
using FreshCart.Identity.Application.Common.Abstractions;
using FreshCart.Identity.Domain.AuditEvents;
using FreshCart.Identity.Domain.Users;
using FreshCart.Identity.Tests.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FreshCart.Identity.Tests.Account;

public sealed class DisableMultiFactorCommandHandlerTests : IDisposable
{
    private const string ValidVerificationCode = "654321";

    private readonly UserManager<ApplicationUser> _userManager = UserManagerSubstitute.Create();
    private readonly IIdentityAuditLog _auditLog = Substitute.For<IIdentityAuditLog>();
    private readonly ICurrentRequestContext _currentRequest = Substitute.For<ICurrentRequestContext>();
    private readonly DisableMultiFactorCommandHandler _handler;
    private readonly ApplicationUser _user;

    public DisableMultiFactorCommandHandlerTests()
    {
        _user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "customer@freshcart.test",
            DisplayName = "Test Customer",
        };

        _handler = new DisableMultiFactorCommandHandler(
            _userManager,
            _auditLog,
            _currentRequest,
            NullLogger<DisableMultiFactorCommandHandler>.Instance);
    }

    [Fact]
    public async Task DisableTurnsOffMultiFactorAfterVerifyingACurrentCode()
    {
        _userManager.FindByIdAsync(_user.Id.ToString()).Returns(_user);
        _userManager.GetTwoFactorEnabledAsync(_user).Returns(true);
        _userManager
            .VerifyTwoFactorTokenAsync(_user, TokenOptions.DefaultAuthenticatorProvider, ValidVerificationCode)
            .Returns(true);
        _userManager.SetTwoFactorEnabledAsync(_user, false).Returns(IdentityResult.Success);

        await _handler
            .Handle(new DisableMultiFactorCommand(_user.Id, ValidVerificationCode), CancellationToken.None);

        await _userManager.Received(1).SetTwoFactorEnabledAsync(_user, false);
        await _userManager.Received(1).UpdateAsync(_user);
        await _auditLog.Received(1).RecordAsync(
            Arg.Is<AuditEvent>(auditEvent =>
                auditEvent.EventType == AuditEventType.MultiFactorDisabled && auditEvent.UserId == _user.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisableThrowsBadRequestAndAuditsWhenCodeIsIncorrect()
    {
        _userManager.FindByIdAsync(_user.Id.ToString()).Returns(_user);
        _userManager.GetTwoFactorEnabledAsync(_user).Returns(true);
        _userManager
            .VerifyTwoFactorTokenAsync(_user, TokenOptions.DefaultAuthenticatorProvider, ValidVerificationCode)
            .Returns(false);

        var disabling = () => _handler.Handle(
            new DisableMultiFactorCommand(_user.Id, ValidVerificationCode),
            CancellationToken.None);

        await disabling.Should().ThrowAsync<BadRequestException>();
        await _userManager.DidNotReceive()
            .SetTwoFactorEnabledAsync(Arg.Any<ApplicationUser>(), Arg.Any<bool>());
        await _auditLog.Received(1).RecordAsync(
            Arg.Is<AuditEvent>(auditEvent =>
                auditEvent.EventType == AuditEventType.MultiFactorVerificationFailed
                && auditEvent.UserId == _user.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisableThrowsBadRequestWhenMultiFactorIsNotEnabled()
    {
        _userManager.FindByIdAsync(_user.Id.ToString()).Returns(_user);
        _userManager.GetTwoFactorEnabledAsync(_user).Returns(false);

        var disabling = () => _handler.Handle(
            new DisableMultiFactorCommand(_user.Id, ValidVerificationCode),
            CancellationToken.None);

        await disabling.Should().ThrowAsync<BadRequestException>();
        await _userManager.DidNotReceive().VerifyTwoFactorTokenAsync(
            Arg.Any<ApplicationUser>(),
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public Task DisableThrowsNotFoundWhenUserDoesNotExist()
    {
        _userManager.FindByIdAsync(Arg.Any<string>()).Returns((ApplicationUser?)null);

        var disabling = () => _handler.Handle(
            new DisableMultiFactorCommand(Guid.NewGuid(), ValidVerificationCode),
            CancellationToken.None);

        return disabling.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DisableThrowsInternalServerWhenDisablingTwoFactorFails()
    {
        _userManager.FindByIdAsync(_user.Id.ToString()).Returns(_user);
        _userManager.GetTwoFactorEnabledAsync(_user).Returns(true);
        _userManager
            .VerifyTwoFactorTokenAsync(_user, TokenOptions.DefaultAuthenticatorProvider, ValidVerificationCode)
            .Returns(true);
        _userManager.SetTwoFactorEnabledAsync(_user, false).Returns(IdentityResult.Failed());

        var disabling = () => _handler.Handle(
            new DisableMultiFactorCommand(_user.Id, ValidVerificationCode),
            CancellationToken.None);

        await disabling.Should().ThrowAsync<InternalServerException>();
        await _auditLog.DidNotReceive().RecordAsync(
            Arg.Is<AuditEvent>(auditEvent => auditEvent.EventType == AuditEventType.MultiFactorDisabled),
            Arg.Any<CancellationToken>());
    }

    public void Dispose() => _userManager.Dispose();
}
