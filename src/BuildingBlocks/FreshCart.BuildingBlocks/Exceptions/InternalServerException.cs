namespace FreshCart.BuildingBlocks.Exceptions;

/// <summary>
/// Catch-all internal failure that the caller cannot act on. Mapped to HTTP 500 by
/// <c>CustomExceptionHandler</c>. The message intentionally exposes only generic detail because
/// stack traces and root causes belong in the structured log, not the wire response.
/// </summary>
public sealed class InternalServerException : Exception
{
    public InternalServerException(string message)
        : base(message)
    {
    }

    public InternalServerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
