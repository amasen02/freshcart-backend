using FluentValidation;

namespace FreshCart.Reporting.Application.Invoices.Commands.GenerateInvoice;

internal sealed class InvoiceLineRequestValidator : AbstractValidator<InvoiceLineRequest>
{
    public InvoiceLineRequestValidator()
    {
        RuleFor(line => line.ProductSku).NotEmpty().MaximumLength(64);
        RuleFor(line => line.ProductName).NotEmpty().MaximumLength(256);
        RuleFor(line => line.Quantity).GreaterThan(0);
        RuleFor(line => line.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(line => line.DiscountAmount).GreaterThanOrEqualTo(0);
        RuleFor(line => line.TaxAmount).GreaterThanOrEqualTo(0);
    }
}
