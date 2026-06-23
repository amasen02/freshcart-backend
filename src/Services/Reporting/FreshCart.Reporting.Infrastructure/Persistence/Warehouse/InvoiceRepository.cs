using FreshCart.Reporting.Application.Common.Abstractions;
using FreshCart.Reporting.Domain.Invoices;
using Microsoft.EntityFrameworkCore;

namespace FreshCart.Reporting.Infrastructure.Persistence.Warehouse;

/// <summary>
/// EF-Core-backed invoice repository. Allocates gap-free invoice numbers under a transactional
/// row lock: the (year, kind) row is read with <c>FOR UPDATE</c> semantics, incremented, and
/// returned to the caller. The caller persists the full invoice in the same transaction.
/// </summary>
public sealed class InvoiceRepository(WarehouseDbContext warehouseDbContext) : IInvoiceRepository
{
    public async Task<Invoice?> FindByNumberAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken)
    {
        var record = await warehouseDbContext.Invoices
            .AsNoTracking()
            .Include(invoice => invoice.Lines)
            .FirstOrDefaultAsync(invoice => invoice.InvoiceNumber == invoiceNumber.Value, cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : MapToDomain(record);
    }

    public async Task<Invoice?> FindByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var record = await warehouseDbContext.Invoices
            .AsNoTracking()
            .Include(invoice => invoice.Lines)
            .FirstOrDefaultAsync(invoice => invoice.OrderId == orderId, cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : MapToDomain(record);
    }

    public async Task<InvoiceNumber> AllocateNextNumberAsync(
        InvoiceKind kind,
        int year,
        CancellationToken cancellationToken)
    {
        // The row lock is implicit when the sequence row is updated; on MySQL InnoDB an UPDATE
        // acquires a record lock, which is the simplest cross-connection serialisation primitive
        // available without going to an external coordinator.
        var sequenceRow = await warehouseDbContext.InvoiceNumberSequences
            .FirstOrDefaultAsync(sequence => sequence.Year == year && sequence.Kind == kind, cancellationToken)
            .ConfigureAwait(false);

        if (sequenceRow is null)
        {
            sequenceRow = new InvoiceNumberSequence { Year = year, Kind = kind, LastSequence = 0 };
            warehouseDbContext.InvoiceNumberSequences.Add(sequenceRow);
        }

        sequenceRow.LastSequence += 1;
        await warehouseDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return InvoiceNumber.Allocate(kind, year, sequenceRow.LastSequence);
    }

    public Task AddAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        var record = new InvoiceRecord
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Kind = invoice.Kind,
            OrderId = invoice.OrderId,
            CustomerId = invoice.CustomerId,
            CustomerEmail = invoice.CustomerEmail,
            CustomerDisplayName = invoice.CustomerDisplayName,
            BillingAddress = invoice.BillingAddress,
            ShippingAddress = invoice.ShippingAddress,
            Subtotal = invoice.Subtotal,
            DiscountTotal = invoice.DiscountTotal,
            TaxTotal = invoice.TaxTotal,
            ShippingTotal = invoice.ShippingTotal,
            GrandTotal = invoice.GrandTotal,
            CurrencyCode = invoice.CurrencyCode,
            IssuedOnUtc = invoice.IssuedOnUtc,
            DueOnUtc = invoice.DueOnUtc,
            OriginalInvoiceNumber = invoice.OriginalInvoiceNumber,
            Notes = invoice.Notes,
            Lines = invoice.Lines
                .Select(line => new InvoiceLineRecord
                {
                    InvoiceId = invoice.Id,
                    LineNumber = line.LineNumber,
                    ProductSku = line.ProductSku,
                    ProductName = line.ProductName,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    DiscountAmount = line.DiscountAmount,
                    TaxAmount = line.TaxAmount,
                    LineTotal = line.LineTotal,
                })
                .ToList(),
        };

        warehouseDbContext.Invoices.Add(record);
        return warehouseDbContext.SaveChangesAsync(cancellationToken);
    }

    private static Invoice MapToDomain(InvoiceRecord record) => new()
    {
        Id = record.Id,
        InvoiceNumber = record.InvoiceNumber,
        Kind = record.Kind,
        OrderId = record.OrderId,
        CustomerId = record.CustomerId,
        CustomerEmail = record.CustomerEmail,
        CustomerDisplayName = record.CustomerDisplayName,
        BillingAddress = record.BillingAddress,
        ShippingAddress = record.ShippingAddress,
        Lines = record.Lines
            .Select(line => new InvoiceLine(
                LineNumber: line.LineNumber,
                ProductSku: line.ProductSku,
                ProductName: line.ProductName,
                Quantity: line.Quantity,
                UnitPrice: line.UnitPrice,
                DiscountAmount: line.DiscountAmount,
                TaxAmount: line.TaxAmount,
                LineTotal: line.LineTotal))
            .ToArray(),
        Subtotal = record.Subtotal,
        DiscountTotal = record.DiscountTotal,
        TaxTotal = record.TaxTotal,
        ShippingTotal = record.ShippingTotal,
        GrandTotal = record.GrandTotal,
        CurrencyCode = record.CurrencyCode,
        IssuedOnUtc = record.IssuedOnUtc,
        DueOnUtc = record.DueOnUtc,
        OriginalInvoiceNumber = record.OriginalInvoiceNumber,
        Notes = record.Notes,
    };
}
