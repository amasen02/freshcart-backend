using FluentValidation;

namespace FreshCart.Identity.Application.Account.Commands.DisableMultiFactor;

public sealed class DisableMultiFactorCommandValidator : AbstractValidator<DisableMultiFactorCommand>
{
    public DisableMultiFactorCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();

        RuleFor(command => command.VerificationCode)
            .NotEmpty()
            .Matches("^[0-9]{6}$")
            .WithMessage("Verification code must be six digits.");
    }
}
