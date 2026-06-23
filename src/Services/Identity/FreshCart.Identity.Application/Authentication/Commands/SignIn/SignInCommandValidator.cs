using FluentValidation;

namespace FreshCart.Identity.Application.Authentication.Commands.SignIn;

public sealed class SignInCommandValidator : AbstractValidator<SignInCommand>
{
    public SignInCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(command => command.MultiFactorCode)
            .Matches("^[0-9]{6}$")
            .When(command => !string.IsNullOrWhiteSpace(command.MultiFactorCode))
            .WithMessage("Multi-factor code must be six digits.");
    }
}
