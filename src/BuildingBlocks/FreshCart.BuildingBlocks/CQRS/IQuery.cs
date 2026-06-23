using MediatR;

namespace FreshCart.BuildingBlocks.CQRS;

/// <summary>
/// Marks a read-only request. Implementations must be side-effect free.
/// </summary>
/// <typeparam name="TResponse">The shape of the data returned to the caller.</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>
    where TResponse : notnull;
