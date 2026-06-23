using FluentAssertions;
using FreshCart.Identity.Application.Account.Commands.EnrollMultiFactor;
using Xunit;

namespace FreshCart.Identity.Tests.Account;

public sealed class EnrollMultiFactorCommandValidatorTests
{
    private readonly EnrollMultiFactorCommandValidator _validator = new();

    [Fact]
    public void CommandWithUserIdIsValid()
    {
        var validationResult = _validator.Validate(new EnrollMultiFactorCommand(Guid.NewGuid()));

        validationResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyUserIdFailsValidation()
    {
        var validationResult = _validator.Validate(new EnrollMultiFactorCommand(Guid.Empty));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(failure =>
            failure.PropertyName == nameof(EnrollMultiFactorCommand.UserId));
    }
}
