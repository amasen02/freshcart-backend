using FluentValidation;
using FreshCart.BuildingBlocks.CQRS;
using MediatR;

namespace FreshCart.BuildingBlocks.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs every registered <see cref="IValidator{T}"/> against the
/// command before the handler executes. Failures are collected into a single
/// <see cref="ValidationException"/> which the global exception handler maps to a 400
/// ProblemDetails response. The behavior targets commands only; queries validate at the route
/// layer.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        var registeredValidators = validators as IList<IValidator<TRequest>> ?? validators.ToArray();
        if (registeredValidators.Count == 0)
        {
            return await next().ConfigureAwait(false);
        }

        var validationContext = new ValidationContext<TRequest>(request);
        var validationResults = await Task
            .WhenAll(registeredValidators.Select(validator => validator.ValidateAsync(validationContext, cancellationToken)))
            .ConfigureAwait(false);

        var failures = validationResults
            .Where(result => result.Errors.Count > 0)
            .SelectMany(result => result.Errors)
            .ToList();

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next().ConfigureAwait(false);
    }
}
