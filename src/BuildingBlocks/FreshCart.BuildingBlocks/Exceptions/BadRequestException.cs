namespace FreshCart.BuildingBlocks.Exceptions;

/// <summary>
/// Thrown when the caller's request is syntactically valid but semantically incorrect for the current
/// application state (for example, attempting to confirm an order that has already been cancelled).
/// Mapped to HTTP 400 by <c>CustomExceptionHandler</c>.
/// </summary>
public sealed class BadRequestException : Exception
{
    public BadRequestException(string message)
        : base(message)
    {
    }

    public BadRequestException(string message, string detail)
        : base(message)
    {
        Detail = detail;
    }

    public string? Detail { get; }
}
