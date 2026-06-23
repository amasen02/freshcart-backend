using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FreshCart.BuildingBlocks.Behaviors;

/// <summary>
/// MediatR pipeline behavior that emits structured start / end events and warns when a handler
/// runs longer than <see cref="SlowHandlerThreshold"/>. The threshold is intentionally constant;
/// services with different SLOs swap the behavior in their own composition root.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
    where TResponse : notnull
{
    private static readonly TimeSpan SlowHandlerThreshold = TimeSpan.FromSeconds(3);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        var requestName = typeof(TRequest).Name;
        LoggingBehaviorLog.RequestStarted(logger, requestName, typeof(TResponse).Name);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next().ConfigureAwait(false);
            stopwatch.Stop();

            if (stopwatch.Elapsed > SlowHandlerThreshold)
            {
                LoggingBehaviorLog.SlowRequestCompleted(
                    logger,
                    requestName,
                    stopwatch.ElapsedMilliseconds,
                    (long)SlowHandlerThreshold.TotalMilliseconds);
            }
            else
            {
                LoggingBehaviorLog.RequestCompleted(logger, requestName, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception handlerFailure) when (RecordFailure(handlerFailure, requestName, stopwatch))
        {
            // Unreachable: RecordFailure always returns false, so the original exception keeps
            // unwinding with its stack intact instead of being caught and rethrown.
            throw;
        }
    }

    private bool RecordFailure(Exception handlerFailure, string requestName, Stopwatch stopwatch)
    {
        stopwatch.Stop();
        LoggingBehaviorLog.RequestFailed(logger, handlerFailure, requestName, stopwatch.ElapsedMilliseconds);
        return false;
    }
}
