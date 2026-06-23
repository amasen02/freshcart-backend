using FluentValidation;

namespace FreshCart.Identity.Application.Account.Commands.EnrollMultiFactor;

public sealed class EnrollMultiFactorCommandValidator : AbstractValidator<EnrollMultiFactorCommand>
{
    public EnrollMultiFactorCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();
    }
}
