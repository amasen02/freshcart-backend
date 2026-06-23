using Microsoft.Extensions.Logging;

namespace FreshCart.BuildingBlocks.Behaviors;

/// <summary>
/// Source-generated log methods for <see cref="LoggingBehavior{TRequest, TResponse}"/>. They live in a
/// non-generic companion type because the LoggerMessage generator cannot emit into generic types.
/// </summary>
internal static partial class LoggingBehaviorLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Handling request {RequestName} expecting response {ResponseName}")]
    internal static partial void RequestStarted(ILogger logger, string requestName, string responseName);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Handled {RequestName} in {ElapsedMilliseconds} ms")]
    internal static partial void RequestCompleted(ILogger logger, string requestName, long elapsedMilliseconds);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "Slow handler detected. {RequestName} took {ElapsedMilliseconds} ms (threshold {ThresholdMilliseconds} ms)")]
    internal static partial void SlowRequestCompleted(ILogger logger, string requestName, long elapsedMilliseconds, long thresholdMilliseconds);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Error, Message = "Request {RequestName} failed after {ElapsedMilliseconds} ms")]
    internal static partial void RequestFailed(ILogger logger, Exception handlerFailure, string requestName, long elapsedMilliseconds);
}
