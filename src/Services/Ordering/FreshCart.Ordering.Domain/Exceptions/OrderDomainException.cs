using FreshCart.BuildingBlocks.Exceptions;

namespace FreshCart.Ordering.Domain.Exceptions;

/// <summary>
/// Raised when an Order invariant or lifecycle rule is violated. Derives from the shared
/// <see cref="DomainException"/> so the platform exception handler maps it to HTTP 422.
/// </summary>
public sealed class OrderDomainException : DomainException
{
    public OrderDomainException(string message)
        : base(message)
    {
    }

    public OrderDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
