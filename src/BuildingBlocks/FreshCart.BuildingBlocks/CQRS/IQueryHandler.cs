using MediatR;

namespace FreshCart.BuildingBlocks.CQRS;

/// <summary>
/// Handles a query and returns a typed read model.
/// </summary>
public interface IQueryHandler<in TQuery, TResponse>
    : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
    where TResponse : notnull;
