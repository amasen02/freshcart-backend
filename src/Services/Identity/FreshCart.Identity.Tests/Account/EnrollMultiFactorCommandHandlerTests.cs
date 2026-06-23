using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Identity.Application.Account.Commands.EnrollMultiFactor;
using FreshCart.Identity.Application.Common.Abstractions;
using FreshCart.Identity.Domain.AuditEvents;
using FreshCart.Identity.Domain.Users;
using FreshCart.Identity.Tests.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FreshCart.Identity.Tests.Account;

public sealed class EnrollMultiFactorCommandHandlerTests : IDisposable
{
    private readonly UserManager<ApplicationUser> _userManager = UserManagerSubstitute.Create();
    private readonly IIdentityAuditLog _auditLog = Substitute.For<IIdentityAuditLog>();
    private readonly ICurrentRequestContext _currentRequest = Substitute.For<ICurrentRequestContext>();
    private readonly EnrollMultiFactorCommandHandler _handler;
    private readonly ApplicationUser _user;

    public EnrollMultiFactorCommandHandlerTests()
    {
        _user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "customer@freshcart.test",
            DisplayName = "Test Customer",
        };

        _handler = new EnrollMultiFactorCommandHandler(
            _userManager,
            _auditLog,
            _currentRequest,
            NullLogger<EnrollMultiFactorCommandHandler>.Instance);
    }

    [Fact]
    public async Task EnrollResetsAuthenticatorKeyAndReturnsGroupedSharedKeyWithOtpauthUri()
    {
        _userManager.FindByIdAsync(_user.Id.ToString()).Returns(_user);
        _userManager.GetTwoFactorEnabledAsync(_user).Returns(false);
        _userManager.ResetAuthenticatorKeyAsync(_user).Returns(IdentityResult.Success);
        _userManager.GetAuthenticatorKeyAsync(_user).Returns("ABCDEFGHIJKLMNOPQRST");

        var result = await _handler
            .Handle(new EnrollMultiFactorCommand(_user.Id), CancellationToken.None);

        result.SharedKey.Should().Be("ABCD EFGH IJKL MNOP QRST");
        result.AuthenticatorUri.OriginalString.Should().Be(
            "otpauth://totp/FreshCart:customer%40freshcart.test?secret=ABCDEFGHIJKLMNOPQRST&issuer=FreshCart&digits=6");
        await _userManager.Received(1).ResetAuthenticatorKeyAsync(_user);
        await _auditLog.Received(1).RecordAsync(
            Arg.Is<AuditEvent>(auditEvent =>
                auditEvent.EventType == AuditEventType.MultiFactorEnrollmentStarted
                && auditEvent.UserId == _user.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnrollKeepsTheTrailingShortGroupWhenKeyLengthIsNotAMultipleOfFour()
    {
        _userManager.FindByIdAsync(_user.Id.ToString()).Returns(_user);
        _userManager.GetTwoFactorEnabledAsync(_user).Returns(false);
        _userManager.ResetAuthenticatorKeyAsync(_user).Returns(IdentityResult.Success);
        _userManager.GetAuthenticatorKeyAsync(_user).Returns("ABCDEFGHIJ");

        var result = await _handler
            .Handle(new EnrollMultiFactorCommand(_user.Id), CancellationToken.None);

        result.SharedKey.Should().Be("ABCD EFGH IJ");
    }

    [Fact]
    public async Task EnrollThrowsNotFoundWhenUserDoesNotExist()
    {
        _userManager.FindByIdAsync(Arg.Any<string>()).Returns((ApplicationUser?)null);

        var enrolling = () => _handler.Handle(new EnrollMultiFactorCommand(Guid.NewGuid()), CancellationToken.None);

        await enrolling.Should().ThrowAsync<NotFoundException>();
        await _userManager.DidNotReceive().ResetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>());
    }

    [Fact]
    public async Task EnrollThrowsBadRequestWhenMultiFactorIsAlreadyEnabled()
    {
        _userManager.FindByIdAsync(_user.Id.ToString()).Returns(_user);
        _userManager.GetTwoFactorEnabledAsync(_user).Returns(true);

        var enrolling = () => _handler.Handle(new EnrollMultiFactorCommand(_user.Id), CancellationToken.None);

        await enrolling.Should().ThrowAsync<BadRequestException>();
        await _userManager.DidNotReceive().ResetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>());
    }

    [Fact]
    public Task EnrollThrowsInternalServerWhenKeyResetFails()
    {
        _userManager.FindByIdAsync(_user.Id.ToString()).Returns(_user);
        _userManager.GetTwoFactorEnabledAsync(_user).Returns(false);
        _userManager.ResetAuthenticatorKeyAsync(_user).Returns(IdentityResult.Failed());

        var enrolling = () => _handler.Handle(new EnrollMultiFactorCommand(_user.Id), CancellationToken.None);

        return enrolling.Should().ThrowAsync<InternalServerException>();
    }

    [Fact]
    public async Task EnrollThrowsInternalServerWhenKeyIsUnavailableAfterReset()
    {
        _userManager.FindByIdAsync(_user.Id.ToString()).Returns(_user);
        _userManager.GetTwoFactorEnabledAsync(_user).Returns(false);
        _userManager.ResetAuthenticatorKeyAsync(_user).Returns(IdentityResult.Success);
        _userManager.GetAuthenticatorKeyAsync(_user).Returns((string?)null);

        var enrolling = () => _handler.Handle(new EnrollMultiFactorCommand(_user.Id), CancellationToken.None);

        await enrolling.Should().ThrowAsync<InternalServerException>();
        await _auditLog.DidNotReceive()
            .RecordAsync(Arg.Any<AuditEvent>(), Arg.Any<CancellationToken>());
    }

    public void Dispose() => _userManager.Dispose();
}
