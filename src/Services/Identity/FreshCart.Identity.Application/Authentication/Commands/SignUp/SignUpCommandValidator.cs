using FluentValidation;

namespace FreshCart.Identity.Application.Authentication.Commands.SignUp;

/// <summary>
/// Server-side validation rules for <see cref="SignUpCommand"/>. The Angular client mirrors these
/// rules in its reactive form so the user receives immediate feedback, but the server is the single
/// source of truth.
/// </summary>
public sealed class SignUpCommandValidator : AbstractValidator<SignUpCommand>
{
    private const int MinimumPasswordLength = 12;

    private const int MinimumCharacterClassCount = 3;

    private const int MaximumPasswordLength = 128;

    public SignUpCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid address.")
            .MaximumLength(256);

        RuleFor(command => command.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MinimumLength(2)
            .MaximumLength(64);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MinimumLength(MinimumPasswordLength)
                .WithMessage($"Password must be at least {MinimumPasswordLength} characters long.")
            .MaximumLength(MaximumPasswordLength)
            .Must(ContainCharactersFromAtLeastThreeClasses)
                .WithMessage("Password must contain characters from at least three of: lower case, upper case, digits, symbols.");
    }

    private static bool ContainCharactersFromAtLeastThreeClasses(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        var hasLower = password.Any(char.IsLower);
        var hasUpper = password.Any(char.IsUpper);
        var hasDigit = password.Any(char.IsDigit);
        var hasSymbol = password.Any(character => !char.IsLetterOrDigit(character));

        var distinctClasses =
            (hasLower ? 1 : 0) + (hasUpper ? 1 : 0) + (hasDigit ? 1 : 0) + (hasSymbol ? 1 : 0);

        return distinctClasses >= MinimumCharacterClassCount;
    }
}
