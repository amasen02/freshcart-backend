using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FreshCart.BuildingBlocks.Messaging.Outbox;

/// <summary>
/// Background worker that drains the <see cref="IOutboxStore"/> and publishes each message onto the
/// MassTransit bus. Owned by the service that registers it; each service tunes the
/// <see cref="OutboxPublisherOptions"/> for its own throughput profile.
/// </summary>
/// <remarks>
/// The worker resolves a fresh DI scope per cycle so the EF Core DbContext (or whatever the
/// <see cref="IOutboxStore"/> wraps) is not held for the lifetime of the host. A message that keeps
/// failing is dead-lettered after <see cref="OutboxPublisherOptions.MaxRetryAttempts"/> attempts so a
/// single poison message cannot wedge the queue; each dead-letter is surfaced at Critical level.
/// </remarks>
public sealed partial class OutboxPublisher(
    IServiceScopeFactory serviceScopeFactory,
    IPublishEndpoint publishEndpoint,
    IOptions<OutboxPublisherOptions> publisherOptions,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions DeserializerOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = publisherOptions.Value;
        LogPublisherStarting(options.BatchSize, options.PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DrainOnceAsync(options.BatchSize, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // expected on shutdown
            }
            catch (Exception drainFailure)
            {
                LogDrainCycleFailed(drainFailure);
            }

            try
            {
                await Task.Delay(options.PollInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task DrainOnceAsync(int batchSize, CancellationToken cancellationToken)
    {
        using var serviceScope = serviceScopeFactory.CreateScope();
        var outboxStore = serviceScope.ServiceProvider.GetRequiredService<IOutboxStore>();

        var pendingMessages = await outboxStore
            .GetUnpublishedAsync(batchSize, cancellationToken)
            .ConfigureAwait(false);

        if (pendingMessages.Count == 0)
        {
            return;
        }

        var publishedSuccessfully = new List<OutboxMessage>(pendingMessages.Count);

        foreach (var pendingMessage in pendingMessages)
        {
            var published = await TryPublishAsync(outboxStore, pendingMessage, cancellationToken).ConfigureAwait(false);
            if (published)
            {
                publishedSuccessfully.Add(pendingMessage);
            }
        }

        if (publishedSuccessfully.Count > 0)
        {
            await outboxStore.MarkAsPublishedAsync(publishedSuccessfully, cancellationToken).ConfigureAwait(false);
            LogPublishedBatch(publishedSuccessfully.Count);
        }
    }

    private async Task<bool> TryPublishAsync(
        IOutboxStore outboxStore,
        OutboxMessage pendingMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var concreteType = Type.GetType(pendingMessage.EventType, throwOnError: false);
            if (concreteType is null)
            {
                LogUnresolvableEventType(pendingMessage.EventType, pendingMessage.Id);
                await RecordFailureAsync(
                        outboxStore,
                        pendingMessage,
                        $"Cannot resolve event type \"{pendingMessage.EventType}\".",
                        cancellationToken)
                    .ConfigureAwait(false);
                return false;
            }

            var integrationEvent = JsonSerializer.Deserialize(
                pendingMessage.ContentJson,
                concreteType,
                DeserializerOptions);

            if (integrationEvent is null)
            {
                LogNullPayload(pendingMessage.Id);
                await RecordFailureAsync(outboxStore, pendingMessage, "Outbox payload deserialised to null.", cancellationToken)
                    .ConfigureAwait(false);
                return false;
            }

            await publishEndpoint.Publish(integrationEvent, concreteType, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception publishFailure)
        {
            LogPublishFailed(publishFailure, pendingMessage.Id, pendingMessage.EventType);
            await RecordFailureAsync(outboxStore, pendingMessage, publishFailure.Message, cancellationToken).ConfigureAwait(false);
            return false;
        }
    }

    private async Task RecordFailureAsync(
        IOutboxStore outboxStore,
        OutboxMessage message,
        string error,
        CancellationToken cancellationToken)
    {
        await outboxStore
            .MarkAsFailedAsync(message, error, publisherOptions.Value.MaxRetryAttempts, cancellationToken)
            .ConfigureAwait(false);

        if (message.IsDeadLettered)
        {
            LogMessageDeadLettered(message.Id, message.EventType, message.RetryAttempt);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Outbox publisher starting with batch size {BatchSize} and poll interval {PollInterval}")]
    private partial void LogPublisherStarting(int batchSize, TimeSpan pollInterval);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Outbox drain cycle failed; will retry after the poll interval")]
    private partial void LogDrainCycleFailed(Exception exception);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Cannot resolve event type {EventType} for outbox message {OutboxMessageId}")]
    private partial void LogUnresolvableEventType(string eventType, Guid outboxMessageId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Outbox message {OutboxMessageId} payload deserialised to null")]
    private partial void LogNullPayload(Guid outboxMessageId);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Failed to publish outbox message {OutboxMessageId} of type {EventType}")]
    private partial void LogPublishFailed(Exception exception, Guid outboxMessageId, string eventType);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Outbox published {PublishedCount} messages")]
    private partial void LogPublishedBatch(int publishedCount);

    [LoggerMessage(EventId = 7, Level = LogLevel.Critical, Message = "Outbox message {OutboxMessageId} of type {EventType} dead-lettered after {RetryAttempt} attempts; it will no longer be retried and needs manual intervention")]
    private partial void LogMessageDeadLettered(Guid outboxMessageId, string eventType, int retryAttempt);
}
