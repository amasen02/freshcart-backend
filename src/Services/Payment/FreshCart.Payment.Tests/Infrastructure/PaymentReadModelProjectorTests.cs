using System.Globalization;
using FluentAssertions;
using FreshCart.Payment.Application.Abstractions;
using FreshCart.Payment.Application.Payments.Models;
using FreshCart.Payment.Domain;
using FreshCart.Payment.Domain.Events;
using FreshCart.Payment.Infrastructure.EventStore;
using FreshCart.Payment.Infrastructure.Projections;
using FreshCart.Payment.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using NSubstitute;
using Xunit;

namespace FreshCart.Payment.Tests.Infrastructure;

/// <summary>
/// Proves the background projector half of PAY-003: it drains the projection-outbox into the read model,
/// projecting a stream's latest state (so the several markers a capture accumulates collapse to one
/// idempotent upsert), stops handing out a processed marker, and releases — rather than dead-letters — a
/// marker whose projection fails so the read model converges on a later cycle. The SQL read-model writer
/// is substituted so the test isolates the projector's claim/replay/mark logic against a real replica set.
/// </summary>
[Collection(MongoEventStoreFixture.CollectionName)]
public sealed class PaymentReadModelProjectorTests : IDisposable
{
    private static readonly Guid OrderId = Guid.Parse("d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1");
    private static readonly Guid CustomerId = Guid.Parse("d2d2d2d2-d2d2-d2d2-d2d2-d2d2d2d2d2d2");
    private static readonly DateTimeOffset Instant = new(2026, 6, 5, 8, 0, 0, TimeSpan.Zero);

    private const decimal Amount = 73.50m;
    private const string CurrencyCode = "GBP";
    private const string CardMethod = "card";
    private const string ProviderReference = "SIM-TEST-REFERENCE";

    private readonly MongoClient _mongoClient;
    private readonly MongoPaymentEventStore _eventStore;
    private readonly MongoPaymentProjectionOutbox _projectionOutbox;
    private readonly IPaymentReadModelWriter _readModelWriter = Substitute.For<IPaymentReadModelWriter>();
    private readonly PaymentReadModelProjector _projector;

    public PaymentReadModelProjectorTests(MongoEventStoreFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _mongoClient = new MongoClient(fixture.ConnectionString);
        var isolatedDatabaseName = $"paymentevents_{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}";
        var database = _mongoClient.GetDatabase(isolatedDatabaseName);
        _eventStore = new MongoPaymentEventStore(_mongoClient, database);
        _projectionOutbox = new MongoPaymentProjectionOutbox(database, TimeProvider.System);
        _projector = new PaymentReadModelProjector(
            _projectionOutbox,
            _eventStore,
            _readModelWriter,
            NullLogger<PaymentReadModelProjector>.Instance);
    }

    public void Dispose() => _mongoClient.Dispose();

    [Fact]
    public async Task ProjectsTheLatestStreamStateOnceAndStopsHandingOutProcessedMarkers()
    {
        var projectedModels = new List<PaymentReadModel>();
        _readModelWriter
            .UpsertAsync(Arg.Do<PaymentReadModel>(projectedModels.Add), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var paymentId = await AppendCapturedStreamAsync();

        var projectedStreamCount = await _projector.ProjectPendingAsync(50, CancellationToken.None);

        projectedStreamCount.Should().Be(1, "three markers for one stream collapse to a single idempotent projection");
        var model = projectedModels.Should().ContainSingle().Subject;
        model.PaymentId.Should().Be(paymentId);
        model.Status.Should().Be(PaymentStatus.Captured);
        model.RefundedAmount.Should().Be(0m);

        projectedModels.Clear();
        var secondDrain = await _projector.ProjectPendingAsync(50, CancellationToken.None);
        secondDrain.Should().Be(0, "the markers were marked processed and are no longer handed out");
        projectedModels.Should().BeEmpty();
    }

    [Fact]
    public async Task AFailedProjectionReleasesTheClaimSoTheNextCycleRetriesAndConverges()
    {
        var attempt = 0;
        _readModelWriter
            .UpsertAsync(Arg.Any<PaymentReadModel>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                attempt++;
                return attempt == 1
                    ? throw new InvalidOperationException("transient read-model failure")
                    : Task.CompletedTask;
            });
        await AppendCapturedStreamAsync();

        var firstDrain = await _projector.ProjectPendingAsync(50, CancellationToken.None);
        firstDrain.Should().Be(0, "the failed projection released its claim instead of being marked processed");

        var secondDrain = await _projector.ProjectPendingAsync(50, CancellationToken.None);
        secondDrain.Should().Be(1, "the released marker is reclaimed and projected on the next cycle");
    }

    private async Task<Guid> AppendCapturedStreamAsync()
    {
        await _eventStore.EnsureIndexesAsync(CancellationToken.None);
        await _projectionOutbox.EnsureIndexesAsync(CancellationToken.None);

        var paymentId = Guid.NewGuid();
        await _eventStore.AppendAsync(
            OrderId, paymentId, expectedVersion: 0,
            [new PaymentInitiated(paymentId, 1, Instant, OrderId, CustomerId, Amount, CurrencyCode, CardMethod)],
            CancellationToken.None);
        await _eventStore.AppendAsync(
            OrderId, paymentId, expectedVersion: 1,
            [new PaymentAuthorized(paymentId, 2, Instant.AddSeconds(1), ProviderReference)],
            CancellationToken.None);
        await _eventStore.AppendAsync(
            OrderId, paymentId, expectedVersion: 2,
            [new PaymentCaptured(paymentId, 3, Instant.AddSeconds(2))],
            CancellationToken.None);

        return paymentId;
    }
}
