using FluentAssertions;
using FreshCart.Identity.Application.Account.Commands.VerifyMultiFactorEnrollment;
using Xunit;

namespace FreshCart.Identity.Tests.Account;

public sealed class VerifyMultiFactorEnrollmentCommandValidatorTests
{
    private readonly VerifyMultiFactorEnrollmentCommandValidator _validator = new();

    [Fact]
    public void CommandWithSixDigitCodeIsValid()
    {
        var validationResult = _validator.Validate(
            new VerifyMultiFactorEnrollmentCommand(Guid.NewGuid(), "123456"));

        validationResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyUserIdFailsValidation()
    {
        var validationResult = _validator.Validate(
            new VerifyMultiFactorEnrollmentCommand(Guid.Empty, "123456"));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(failure =>
            failure.PropertyName == nameof(VerifyMultiFactorEnrollmentCommand.UserId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12345a")]
    public void NonSixDigitCodeFailsValidation(string verificationCode)
    {
        var validationResult = _validator.Validate(
            new VerifyMultiFactorEnrollmentCommand(Guid.NewGuid(), verificationCode));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(failure =>
            failure.PropertyName == nameof(VerifyMultiFactorEnrollmentCommand.VerificationCode));
    }
}
