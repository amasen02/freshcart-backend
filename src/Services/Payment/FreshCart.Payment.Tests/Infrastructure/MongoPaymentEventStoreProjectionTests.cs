using System.Globalization;
using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Payment.Domain.Events;
using FreshCart.Payment.Infrastructure.EventStore;
using FreshCart.Payment.Infrastructure.Projections;
using FreshCart.Payment.Tests.Common;
using MongoDB.Driver;
using Xunit;

namespace FreshCart.Payment.Tests.Infrastructure;

/// <summary>
/// Proves the PAY-003 fix at the event store: each append stages a projection marker in the same
/// transaction as the events (so a failed append leaks no marker), the one-payment-per-order invariant
/// is enforced at the source of truth, and the order-to-stream lookup that backs idempotent capture
/// reads the event store rather than the asynchronously-projected read model.
/// </summary>
[Collection(MongoEventStoreFixture.CollectionName)]
public sealed class MongoPaymentEventStoreProjectionTests : IDisposable
{
    private static readonly Guid OrderId = Guid.Parse("c1c1c1c1-c1c1-c1c1-c1c1-c1c1c1c1c1c1");
    private static readonly Guid CustomerId = Guid.Parse("c2c2c2c2-c2c2-c2c2-c2c2-c2c2c2c2c2c2");
    private static readonly DateTimeOffset Instant = new(2026, 6, 4, 10, 0, 0, TimeSpan.Zero);

    private const decimal Amount = 64.00m;
    private const string CurrencyCode = "GBP";
    private const string CardMethod = "card";

    private readonly MongoClient _mongoClient;
    private readonly MongoPaymentEventStore _eventStore;
    private readonly IMongoCollection<PaymentProjectionOutboxDocument> _projectionMarkers;

    public MongoPaymentEventStoreProjectionTests(MongoEventStoreFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _mongoClient = new MongoClient(fixture.ConnectionString);
        var isolatedDatabaseName = $"paymentevents_{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}";
        var database = _mongoClient.GetDatabase(isolatedDatabaseName);
        _eventStore = new MongoPaymentEventStore(_mongoClient, database);
        _projectionMarkers = database.GetCollection<PaymentProjectionOutboxDocument>(PaymentProjectionOutboxDocument.CollectionName);
    }

    public void Dispose() => _mongoClient.Dispose();

    [Fact]
    public async Task AppendStagesAProjectionMarkerInTheSameTransactionAsTheEvents()
    {
        await _eventStore.EnsureIndexesAsync(CancellationToken.None);
        var paymentId = Guid.NewGuid();

        await _eventStore.AppendAsync(OrderId, paymentId, expectedVersion: 0, [InitiatedEvent(paymentId)], CancellationToken.None);

        var stream = await _eventStore.LoadStreamAsync(paymentId, CancellationToken.None);
        stream.Should().HaveCount(1);
        var markers = await _projectionMarkers.Find(FilterDefinition<PaymentProjectionOutboxDocument>.Empty).ToListAsync();
        markers.Should().ContainSingle().Which.PaymentId.Should().Be(paymentId);
    }

    [Fact]
    public async Task AVersionConflictStagesNoProjectionMarker()
    {
        await _eventStore.EnsureIndexesAsync(CancellationToken.None);
        var paymentId = Guid.NewGuid();
        await _eventStore.AppendAsync(OrderId, paymentId, expectedVersion: 0, [InitiatedEvent(paymentId)], CancellationToken.None);

        var appendSameVersionAgain = () => _eventStore.AppendAsync(
            OrderId, paymentId, expectedVersion: 0, [InitiatedEvent(paymentId)], CancellationToken.None);

        await appendSameVersionAgain.Should().ThrowAsync<ConflictException>();

        var markers = await _projectionMarkers.Find(FilterDefinition<PaymentProjectionOutboxDocument>.Empty).ToListAsync();
        markers.Should().ContainSingle("the conflicting append's marker rolls back with its events; only the first append's marker survives");
    }

    [Fact]
    public async Task FindStreamIdByOrderIdReturnsTheStreamForAKnownOrderAndNullOtherwise()
    {
        await _eventStore.EnsureIndexesAsync(CancellationToken.None);
        var paymentId = Guid.NewGuid();
        await _eventStore.AppendAsync(OrderId, paymentId, expectedVersion: 0, [InitiatedEvent(paymentId)], CancellationToken.None);

        (await _eventStore.FindStreamIdByOrderIdAsync(OrderId, CancellationToken.None)).Should().Be(paymentId);
        (await _eventStore.FindStreamIdByOrderIdAsync(Guid.NewGuid(), CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task ASecondPaymentForTheSameOrderIsRejectedByTheSourceOfTruth()
    {
        await _eventStore.EnsureIndexesAsync(CancellationToken.None);
        var firstPaymentId = Guid.NewGuid();
        await _eventStore.AppendAsync(OrderId, firstPaymentId, expectedVersion: 0, [InitiatedEvent(firstPaymentId)], CancellationToken.None);

        var secondPaymentId = Guid.NewGuid();
        var appendSecondPaymentForSameOrder = () => _eventStore.AppendAsync(
            OrderId, secondPaymentId, expectedVersion: 0, [InitiatedEvent(secondPaymentId)], CancellationToken.None);

        await appendSecondPaymentForSameOrder.Should().ThrowAsync<ConflictException>();
    }

    private static PaymentInitiated InitiatedEvent(Guid paymentId) => new(
        paymentId, 1, Instant, OrderId, CustomerId, Amount, CurrencyCode, CardMethod);
}
