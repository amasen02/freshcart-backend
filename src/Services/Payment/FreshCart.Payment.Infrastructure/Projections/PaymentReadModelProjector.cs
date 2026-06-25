using FreshCart.Payment.Application.Abstractions;
using FreshCart.Payment.Application.Payments.Models;
using FreshCart.Payment.Domain;
using Microsoft.Extensions.Logging;

namespace FreshCart.Payment.Infrastructure.Projections;

/// <summary>
/// Drains the projection-outbox into the SQL read model. For each claimed marker it replays the payment
/// stream to its latest state and upserts the read model row, so projecting is idempotent and order-free:
/// replaying always yields the current state regardless of how many markers a stream accumulated or in
/// which order they are processed. A stream whose projection fails is released (not dead-lettered) so the
/// read model converges on a later cycle once the underlying problem clears.
/// </summary>
public sealed partial class PaymentReadModelProjector(
    MongoPaymentProjectionOutbox projectionOutbox,
    IPaymentEventStore paymentEventStore,
    IPaymentReadModelWriter paymentReadModelWriter,
    ILogger<PaymentReadModelProjector> logger)
{
    public async Task<int> ProjectPendingAsync(int batchSize, CancellationToken cancellationToken)
    {
        var claimedMarkers = await projectionOutbox.ClaimPendingAsync(batchSize, cancellationToken).ConfigureAwait(false);
        if (claimedMarkers.Count == 0)
        {
            return 0;
        }

        var projectedStreamCount = 0;
        foreach (var streamMarkers in claimedMarkers.GroupBy(marker => marker.PaymentId))
        {
            var markersForStream = streamMarkers.ToList();
            try
            {
                await ProjectStreamAsync(streamMarkers.Key, cancellationToken).ConfigureAwait(false);
                await projectionOutbox.MarkProjectedAsync(markersForStream, cancellationToken).ConfigureAwait(false);
                projectedStreamCount++;
            }
            catch (Exception projectionFailure)
            {
                LogProjectionFailed(projectionFailure, streamMarkers.Key);
                await projectionOutbox.ReleaseAsync(markersForStream, cancellationToken).ConfigureAwait(false);
            }
        }

        return projectedStreamCount;
    }

    private async Task ProjectStreamAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var eventStream = await paymentEventStore.LoadStreamAsync(paymentId, cancellationToken).ConfigureAwait(false);
        if (eventStream.Count == 0)
        {
            return;
        }

        var payment = PaymentAggregate.ReplayFrom(eventStream);
        await paymentReadModelWriter
            .UpsertAsync(PaymentReadModel.FromAggregate(payment), cancellationToken)
            .ConfigureAwait(false);

        LogStreamProjected(paymentId, payment.Status);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Projected payment stream {PaymentId} to read-model status {Status}")]
    private partial void LogStreamProjected(Guid paymentId, PaymentStatus status);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to project payment stream {PaymentId}; the claim was released and will be retried")]
    private partial void LogProjectionFailed(Exception exception, Guid paymentId);
}
