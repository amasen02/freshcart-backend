using FreshCart.Payment.Application.Payments.Models;

namespace FreshCart.Payment.Application.Abstractions;

public interface IPaymentReadModelWriter
{
    Task UpsertAsync(PaymentReadModel payment, CancellationToken cancellationToken);
}
