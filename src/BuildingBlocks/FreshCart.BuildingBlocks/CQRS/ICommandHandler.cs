using MediatR;

namespace FreshCart.BuildingBlocks.CQRS;

/// <summary>
/// Handles a command that produces no meaningful result other than success.
/// </summary>
public interface ICommandHandler<in TCommand>
    : ICommandHandler<TCommand, Unit>
    where TCommand : ICommand<Unit>;

/// <summary>
/// Handles a command that produces a typed result.
/// </summary>
public interface ICommandHandler<in TCommand, TResponse>
    : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
    where TResponse : notnull;
