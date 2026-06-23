using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Reporting.Application.Common.Abstractions;
using FreshCart.Reporting.Domain.Invoices;

namespace FreshCart.Reporting.Application.Invoices.Queries.DownloadInvoice;

public sealed class DownloadInvoiceQueryHandler(
    IInvoiceRepository invoiceRepository,
    IDocumentStore documentStore)
    : IQueryHandler<DownloadInvoiceQuery, DownloadInvoiceResult>
{
    private const string InvoiceContainerName = "invoices";

    private static readonly TimeSpan SharedAccessSignatureValidity = TimeSpan.FromMinutes(15);

    public async Task<DownloadInvoiceResult> Handle(
        DownloadInvoiceQuery query,
        CancellationToken cancellationToken)
    {
        if (!InvoiceNumber.TryParse(query.InvoiceNumber, out var parsed))
        {
            throw new BadRequestException("Invoice number is not in a recognised format.");
        }

        var invoice = await invoiceRepository
            .FindByNumberAsync(parsed, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Invoice", parsed.Value);

        var signedUri = await documentStore
            .CreateReadOnlySharedAccessSignatureAsync(
                InvoiceContainerName,
                $"{invoice.InvoiceNumber}.pdf",
                SharedAccessSignatureValidity,
                cancellationToken)
            .ConfigureAwait(false);

        return new DownloadInvoiceResult(
            InvoiceNumber: invoice.InvoiceNumber,
            SignedDownloadUri: signedUri,
            GrandTotal: invoice.GrandTotal,
            CurrencyCode: invoice.CurrencyCode,
            IssuedOnUtc: invoice.IssuedOnUtc);
    }
}
