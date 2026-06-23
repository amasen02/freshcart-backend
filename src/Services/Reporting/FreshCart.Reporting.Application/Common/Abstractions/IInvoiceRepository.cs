using FreshCart.Reporting.Domain.Invoices;

namespace FreshCart.Reporting.Application.Common.Abstractions;

/// <summary>
/// Persistence port for invoices. Implementations are responsible for the gap-free yearly
/// sequence allocation (under a transactional lock) and for guaranteeing uniqueness of the
/// allocated <see cref="InvoiceNumber"/>.
/// </summary>
public interface IInvoiceRepository
{
    Task<Invoice?> FindByNumberAsync(InvoiceNumber invoiceNumber, CancellationToken cancellationToken);

    Task<Invoice?> FindByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>
    /// Allocates the next sequence in the year+kind tuple under a row lock and returns the
    /// projected number. Callers must then persist the full invoice in the same transaction;
    /// implementations expose a unit-of-work wrapper for that purpose.
    /// </summary>
    Task<InvoiceNumber> AllocateNextNumberAsync(
        InvoiceKind kind,
        int year,
        CancellationToken cancellationToken);

    Task AddAsync(Invoice invoice, CancellationToken cancellationToken);
}
