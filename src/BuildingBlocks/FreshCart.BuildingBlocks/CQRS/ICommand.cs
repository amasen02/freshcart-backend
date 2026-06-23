using MediatR;

namespace FreshCart.BuildingBlocks.CQRS;

/// <summary>
/// Marks a request that changes state.
/// A command always returns a result (even if only <see cref="Unit"/>) so that handlers can communicate
/// outcomes such as the identifier of a newly-created aggregate.
/// </summary>
public interface ICommand : ICommand<Unit>;

/// <summary>
/// Marks a state-changing request that returns a typed result.
/// </summary>
/// <typeparam name="TResponse">The result produced by the corresponding <see cref="ICommandHandler{TCommand, TResponse}"/>.</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>;
