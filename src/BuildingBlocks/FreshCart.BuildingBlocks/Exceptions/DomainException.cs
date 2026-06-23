namespace FreshCart.BuildingBlocks.Exceptions;

/// <summary>
/// Thrown when a domain invariant is violated. Always represents a programmer-detectable bug or a
/// rule-of-the-business violation that the caller is responsible for understanding.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
