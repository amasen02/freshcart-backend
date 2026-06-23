using FluentValidation;
using FreshCart.Reporting.Domain.Invoices;

namespace FreshCart.Reporting.Application.Invoices.Commands.GenerateInvoice;

internal sealed class InvoiceAddressValidator : AbstractValidator<InvoiceAddress>
{
    public InvoiceAddressValidator()
    {
        RuleFor(address => address.FullName).NotEmpty().MaximumLength(128);
        RuleFor(address => address.AddressLine1).NotEmpty().MaximumLength(256);
        RuleFor(address => address.City).NotEmpty().MaximumLength(128);
        RuleFor(address => address.PostalCode).NotEmpty().MaximumLength(32);
        RuleFor(address => address.Country).NotEmpty().Length(2, 64);
    }
}
