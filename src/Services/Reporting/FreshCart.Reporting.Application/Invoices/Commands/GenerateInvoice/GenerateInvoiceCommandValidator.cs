using FluentValidation;

namespace FreshCart.Reporting.Application.Invoices.Commands.GenerateInvoice;

public sealed class GenerateInvoiceCommandValidator : AbstractValidator<GenerateInvoiceCommand>
{
    public GenerateInvoiceCommandValidator()
    {
        RuleFor(command => command.OrderId).NotEmpty();
        RuleFor(command => command.CustomerEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.CustomerDisplayName).NotEmpty().MaximumLength(128);
        RuleFor(command => command.CustomerId).NotEmpty();

        RuleFor(command => command.BillingAddress).NotNull().SetValidator(new InvoiceAddressValidator());
        RuleFor(command => command.ShippingAddress).NotNull().SetValidator(new InvoiceAddressValidator());

        RuleFor(command => command.Lines)
            .NotNull()
            .Must(lines => lines.Count > 0).WithMessage("Invoice must contain at least one line.")
            .ForEach(rule => rule.SetValidator(new InvoiceLineRequestValidator()));

        RuleFor(command => command.DiscountTotal).GreaterThanOrEqualTo(0);
        RuleFor(command => command.TaxTotal).GreaterThanOrEqualTo(0);
        RuleFor(command => command.ShippingTotal).GreaterThanOrEqualTo(0);

        RuleFor(command => command.CurrencyCode).NotEmpty().Length(3);
    }
}
