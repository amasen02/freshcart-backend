namespace FreshCart.BuildingBlocks.Exceptions;

/// <summary>
/// Thrown when the caller is authenticated but the requested action is not permitted for them.
/// Mapped to HTTP 403 by <c>CustomExceptionHandler</c>.
/// </summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message)
        : base(message)
    {
    }
}
