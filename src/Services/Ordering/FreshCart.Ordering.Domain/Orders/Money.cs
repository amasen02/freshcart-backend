using FreshCart.Ordering.Domain.Exceptions;

namespace FreshCart.Ordering.Domain.Orders;

/// <summary>
/// Monetary amount bound to a currency. Arithmetic refuses to mix currencies; an order is always
/// priced in a single currency so a mismatch indicates corrupted upstream data, not user error.
/// </summary>
public sealed record Money(decimal Amount, string CurrencyCode)
{
    public Money Add(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        EnsureSameCurrency(other, "add");
        return this with { Amount = Amount + other.Amount };
    }

    public Money Subtract(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        EnsureSameCurrency(other, "subtract");
        return this with { Amount = Amount - other.Amount };
    }

    public Money MultiplyBy(int factor) => this with { Amount = Amount * factor };

    private void EnsureSameCurrency(Money other, string operation)
    {
        if (!string.Equals(CurrencyCode, other.CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new OrderDomainException(
                $"Cannot {operation} an amount in {other.CurrencyCode} to an amount in {CurrencyCode}.");
        }
    }
}
