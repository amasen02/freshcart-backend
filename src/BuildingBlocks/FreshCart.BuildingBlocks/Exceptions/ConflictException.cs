namespace FreshCart.BuildingBlocks.Exceptions;

/// <summary>
/// Thrown when a uniqueness invariant would be violated (duplicate email, double-charged idempotency key,
/// optimistic-concurrency mismatch). Mapped to HTTP 409 by <c>CustomExceptionHandler</c>.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
