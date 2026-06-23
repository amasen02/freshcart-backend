using FluentValidation;

namespace FreshCart.Payment.Application.Payments.Commands.CapturePayment;

public sealed class CapturePaymentCommandValidator : AbstractValidator<CapturePaymentCommand>
{
    private const int MoneyPrecision = 18;
    private const int MoneyScale = 2;
    private const int MaxMethodLength = 50;
    private const int MaxIdempotencyKeyLength = 100;
    private const string IsoCurrencyCodePattern = "^[A-Z]{3}$";

    public CapturePaymentCommandValidator()
    {
        RuleFor(command => command.OrderId).NotEmpty();

        RuleFor(command => command.CustomerId).NotEmpty();

        RuleFor(command => command.Amount)
            .GreaterThan(0)
            .PrecisionScale(MoneyPrecision, MoneyScale, ignoreTrailingZeros: true);

        RuleFor(command => command.CurrencyCode)
            .NotEmpty()
            .Matches(IsoCurrencyCodePattern)
            .WithMessage("Currency code must be a three-letter uppercase ISO 4217 code.");

        RuleFor(command => command.Method)
            .NotEmpty()
            .MaximumLength(MaxMethodLength);

        RuleFor(command => command.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(MaxIdempotencyKeyLength);
    }
}
