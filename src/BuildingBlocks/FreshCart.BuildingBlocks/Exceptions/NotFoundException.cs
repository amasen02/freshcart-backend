namespace FreshCart.BuildingBlocks.Exceptions;

/// <summary>
/// Thrown when an entity is requested but does not exist.
/// Mapped to HTTP 404 by <c>CustomExceptionHandler</c>.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string entityName, object identifier)
        : base($"Entity \"{entityName}\" with identifier \"{identifier}\" was not found.")
    {
    }
}
