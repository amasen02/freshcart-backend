using Dapper;
using FreshCart.Reporting.Application.Common.Abstractions;
using FreshCart.Reporting.Domain.Invoices;
using Microsoft.EntityFrameworkCore;

namespace FreshCart.Reporting.Infrastructure.Persistence.Warehouse;

/// <summary>
/// EF-Core-backed invoice repository. Invoice numbers are allocated with a single atomic upsert
/// (<see cref="AllocateNextNumberAsync"/>) so that concurrent allocators can never be handed the same
/// number; the rendered invoice is then persisted separately by the caller.
/// </summary>
public sealed class InvoiceRepository(
    WarehouseDbContext warehouseDbContext,
    IWarehouseConnectionFactory warehouseConnectionFactory) : IInvoiceRepository
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
        // Read-then-write in application code hands two concurrent allocators the same number. Instead the
        // upsert increments the (year, kind) counter atomically under InnoDB's row lock and stashes the new
        // value in this connection's session-scoped LAST_INSERT_ID, which the next statement reads back —
        // so each allocator gets its own distinct, gap-free number.
        const string incrementSequenceSql = """
            INSERT INTO invoice_number_sequences (Year, Kind, LastSequence)
            VALUES (@Year, @Kind, LAST_INSERT_ID(1))
            ON DUPLICATE KEY UPDATE LastSequence = LAST_INSERT_ID(LastSequence + 1)
            """;

        const string readBackSequenceSql = "SELECT LAST_INSERT_ID()";

        var connection = await warehouseConnectionFactory
            .CreateOpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        await using (connection.ConfigureAwait(false))
        {
            await connection.ExecuteAsync(new CommandDefinition(
                commandText: incrementSequenceSql,
                parameters: new { Year = year, Kind = (int)kind },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

            var nextSequence = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
                commandText: readBackSequenceSql,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

            return InvoiceNumber.Allocate(kind, year, nextSequence);
        }
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
