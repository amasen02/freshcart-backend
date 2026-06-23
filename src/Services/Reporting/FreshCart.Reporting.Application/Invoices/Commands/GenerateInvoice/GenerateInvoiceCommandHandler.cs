using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Reporting.Application.Common.Abstractions;
using FreshCart.Reporting.Domain.Invoices;
using Microsoft.Extensions.Logging;

namespace FreshCart.Reporting.Application.Invoices.Commands.GenerateInvoice;

/// <summary>
/// Generates an invoice and persists both the metadata record and the rendered PDF. Idempotent:
/// if an invoice already exists for the order the existing record is returned unchanged.
/// </summary>
public sealed class GenerateInvoiceCommandHandler(
    IInvoiceRepository invoiceRepository,
    IInvoiceRenderer invoicePdfRenderer,
    IDocumentStore documentStore,
    TimeProvider timeProvider,
    ILogger<GenerateInvoiceCommandHandler> logger)
    : ICommandHandler<GenerateInvoiceCommand, GenerateInvoiceResult>
{
    private const string InvoiceContainerName = "invoices";

    private const int PaymentTermDays = 14;

    private static readonly TimeSpan SharedAccessSignatureValidity = TimeSpan.FromDays(1);

    public async Task<GenerateInvoiceResult> Handle(
        GenerateInvoiceCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var existingInvoice = await invoiceRepository
            .FindByOrderIdAsync(command.OrderId, cancellationToken)
            .ConfigureAwait(false);

        if (existingInvoice is not null)
        {
            return await ReuseExistingInvoiceAsync(existingInvoice, command.OrderId, cancellationToken)
                .ConfigureAwait(false);
        }

        var nowUtc = timeProvider.GetUtcNow();
        var allocatedNumber = await invoiceRepository
            .AllocateNextNumberAsync(command.Kind, nowUtc.Year, cancellationToken)
            .ConfigureAwait(false);

        var invoice = ComposeInvoice(command, allocatedNumber.Value, nowUtc);

        var renderedPdf = await invoicePdfRenderer
            .RenderAsync(invoice, cancellationToken)
            .ConfigureAwait(false);

        await documentStore
            .StoreAsync(
                InvoiceContainerName,
                BlobNameFor(invoice.InvoiceNumber),
                renderedPdf.Content,
                renderedPdf.ContentType,
                cancellationToken)
            .ConfigureAwait(false);

        await invoiceRepository.AddAsync(invoice, cancellationToken).ConfigureAwait(false);

        var downloadUri = await CreateDownloadUriAsync(invoice.InvoiceNumber, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Generated invoice {InvoiceNumber} for order {OrderId} ({GrandTotal} {CurrencyCode})",
            invoice.InvoiceNumber,
            command.OrderId,
            invoice.GrandTotal,
            invoice.CurrencyCode);

        return new GenerateInvoiceResult(
            InvoiceId: invoice.Id,
            InvoiceNumber: invoice.InvoiceNumber,
            DownloadUri: downloadUri,
            GrandTotal: invoice.GrandTotal,
            CurrencyCode: invoice.CurrencyCode);
    }

    private async Task<GenerateInvoiceResult> ReuseExistingInvoiceAsync(
        Invoice existingInvoice,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Invoice {InvoiceNumber} already exists for order {OrderId}; returning the existing record.",
            existingInvoice.InvoiceNumber,
            orderId);

        var existingUri = await CreateDownloadUriAsync(existingInvoice.InvoiceNumber, cancellationToken)
            .ConfigureAwait(false);

        return new GenerateInvoiceResult(
            InvoiceId: existingInvoice.Id,
            InvoiceNumber: existingInvoice.InvoiceNumber,
            DownloadUri: existingUri,
            GrandTotal: existingInvoice.GrandTotal,
            CurrencyCode: existingInvoice.CurrencyCode);
    }

    private Task<Uri> CreateDownloadUriAsync(string invoiceNumber, CancellationToken cancellationToken) =>
        documentStore.CreateReadOnlySharedAccessSignatureAsync(
            InvoiceContainerName,
            BlobNameFor(invoiceNumber),
            SharedAccessSignatureValidity,
            cancellationToken);

    private static Invoice ComposeInvoice(GenerateInvoiceCommand command, string invoiceNumber, DateTimeOffset nowUtc)
    {
        var lines = BuildLines(command);
        var subtotal = lines.Sum(line => line.UnitPrice * line.Quantity);
        var grandTotal = subtotal - command.DiscountTotal + command.TaxTotal + command.ShippingTotal;

        return new Invoice
        {
            InvoiceNumber = invoiceNumber,
            Kind = command.Kind,
            OrderId = command.OrderId,
            CustomerId = command.CustomerId,
            CustomerEmail = command.CustomerEmail,
            CustomerDisplayName = command.CustomerDisplayName,
            BillingAddress = command.BillingAddress,
            ShippingAddress = command.ShippingAddress,
            Lines = lines,
            Subtotal = subtotal,
            DiscountTotal = command.DiscountTotal,
            TaxTotal = command.TaxTotal,
            ShippingTotal = command.ShippingTotal,
            GrandTotal = grandTotal,
            CurrencyCode = command.CurrencyCode,
            IssuedOnUtc = nowUtc,
            DueOnUtc = nowUtc.AddDays(PaymentTermDays),
            OriginalInvoiceNumber = command.OriginalInvoiceNumber,
            Notes = command.Notes,
        };
    }

    private static InvoiceLine[] BuildLines(GenerateInvoiceCommand command)
    {
        return command.Lines
            .Select((line, zeroBasedIndex) => new InvoiceLine(
                LineNumber: zeroBasedIndex + 1,
                ProductSku: line.ProductSku,
                ProductName: line.ProductName,
                Quantity: line.Quantity,
                UnitPrice: line.UnitPrice,
                DiscountAmount: line.DiscountAmount,
                TaxAmount: line.TaxAmount,
                LineTotal: (line.UnitPrice * line.Quantity) - line.DiscountAmount + line.TaxAmount))
            .ToArray();
    }

    private static string BlobNameFor(string invoiceNumber) => $"{invoiceNumber}.pdf";
}
