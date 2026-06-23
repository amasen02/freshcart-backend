using FluentValidation;

namespace FreshCart.Identity.Application.Account.Commands.VerifyMultiFactorEnrollment;

public sealed class VerifyMultiFactorEnrollmentCommandValidator
    : AbstractValidator<VerifyMultiFactorEnrollmentCommand>
{
    public VerifyMultiFactorEnrollmentCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();

        RuleFor(command => command.VerificationCode)
            .NotEmpty()
            .Matches("^[0-9]{6}$")
            .WithMessage("Verification code must be six digits.");
    }
}
