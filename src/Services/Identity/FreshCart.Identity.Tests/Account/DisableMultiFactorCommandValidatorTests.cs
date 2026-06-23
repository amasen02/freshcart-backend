using FluentAssertions;
using FreshCart.Identity.Application.Account.Commands.DisableMultiFactor;
using Xunit;

namespace FreshCart.Identity.Tests.Account;

public sealed class DisableMultiFactorCommandValidatorTests
{
    private readonly DisableMultiFactorCommandValidator _validator = new();

    [Fact]
    public void CommandWithSixDigitCodeIsValid()
    {
        var validationResult = _validator.Validate(
            new DisableMultiFactorCommand(Guid.NewGuid(), "123456"));

        validationResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyUserIdFailsValidation()
    {
        var validationResult = _validator.Validate(
            new DisableMultiFactorCommand(Guid.Empty, "123456"));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(failure =>
            failure.PropertyName == nameof(DisableMultiFactorCommand.UserId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12345a")]
    public void NonSixDigitCodeFailsValidation(string verificationCode)
    {
        var validationResult = _validator.Validate(
            new DisableMultiFactorCommand(Guid.NewGuid(), verificationCode));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(failure =>
            failure.PropertyName == nameof(DisableMultiFactorCommand.VerificationCode));
    }
}
