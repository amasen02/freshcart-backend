using System.Text.Json;
using FluentAssertions;
using FreshCart.BuildingBlocks.Messaging.Events;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.BuildingBlocks.Messaging.Outbox;
using FreshCart.Delivery.Domain.Deliveries;
using FreshCart.Delivery.Infrastructure.Persistence;
using FreshCart.Delivery.Infrastructure.Persistence.Repositories;
using FreshCart.Delivery.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using DeliveryAggregate = FreshCart.Delivery.Domain.Deliveries.Delivery;

namespace FreshCart.Delivery.Tests.Infrastructure;

/// <summary>
/// Proves the transactional outbox guarantee at the heart of DLV-002/003: the delivery document and the
/// integration event it raises are written in one multi-document transaction, so either both commit or
/// neither does. Runs against the real single-node replica set the fixture boots; a standalone mongod
/// would reject the transaction outright.
/// </summary>
[Collection(MongoIntegrationCollection.Name)]
public sealed class MongoDeliveryUnitOfWorkTests(MongoIntegrationFixture fixture)
{
    private static readonly JsonSerializerOptions OutboxSerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly DateTimeOffset SlotStart = new(2026, 7, 5, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PersistScheduledDeliveryWritesTheDeliveryAndStagesItsEventInOneTransaction()
    {
        var context = fixture.CreateIsolatedContext();
        var unitOfWork = new MongoDeliveryUnitOfWork(fixture.Client, context, TimeProvider.System);
        var delivery = ScheduledDelivery(Guid.NewGuid(), Guid.NewGuid());
        var scheduledEvent = ScheduledEvent(delivery);

        await unitOfWork.PersistScheduledDeliveryAsync(delivery, scheduledEvent, CancellationToken.None);

        var storedDelivery = await new MongoDeliveryRepository(context)
            .FindByOrderIdAsync(delivery.OrderId, CancellationToken.None);
        storedDelivery.Should().NotBeNull();
        storedDelivery!.Id.Should().Be(delivery.Id);

        var stagedMessages = await context.Outbox.Find(FilterDefinition<OutboxMessage>.Empty).ToListAsync();
        stagedMessages.Should().ContainSingle();
        var message = stagedMessages[0];
        message.ProcessedOnUtc.Should().BeNull();
        message.EventType.Should().Be(scheduledEvent.EventType);

        // The staged payload must resolve and deserialise exactly as the OutboxPublisher would, otherwise
        // the event would dead-letter at publish time instead of reaching the bus.
        var resolvedType = EventContractTypeResolver.Resolve(message.EventType);
        resolvedType.Should().Be<DeliveryScheduledIntegrationEvent>();
        var deserialized = (DeliveryScheduledIntegrationEvent)JsonSerializer
            .Deserialize(message.ContentJson, resolvedType!, OutboxSerializerOptions)!;
        deserialized.OrderId.Should().Be(scheduledEvent.OrderId);
        deserialized.DeliveryId.Should().Be(scheduledEvent.DeliveryId);
        deserialized.CustomerId.Should().Be(scheduledEvent.CustomerId);
        deserialized.SlotStartUtc.Should().Be(scheduledEvent.SlotStartUtc);
    }

    [Fact]
    public async Task PersistCompletedDeliveryUpdatesTheDeliveryAndStagesItsEventInOneTransaction()
    {
        var context = fixture.CreateIsolatedContext();
        var repository = new MongoDeliveryRepository(context);
        var unitOfWork = new MongoDeliveryUnitOfWork(fixture.Client, context, TimeProvider.System);

        var delivery = ScheduledDelivery(Guid.NewGuid(), Guid.NewGuid());
        await repository.AddAsync(delivery, CancellationToken.None);
        delivery.StartOutForDelivery();
        delivery.Complete(SlotStart.AddHours(2));

        var completedEvent = new DeliveryCompletedIntegrationEvent
        {
            OrderId = delivery.OrderId,
            DeliveryId = delivery.Id,
            CustomerId = delivery.CustomerId,
            DeliveredOnUtc = SlotStart.AddHours(2),
        };

        await unitOfWork.PersistCompletedDeliveryAsync(delivery, completedEvent, CancellationToken.None);

        var storedDelivery = await repository.FindByIdAsync(delivery.Id, CancellationToken.None);
        storedDelivery!.Status.Should().Be(DeliveryStatus.Completed);
        var stagedMessages = await context.Outbox.Find(FilterDefinition<OutboxMessage>.Empty).ToListAsync();
        stagedMessages.Should().ContainSingle();
        stagedMessages[0].EventType.Should().Be(completedEvent.EventType);
    }

    [Fact]
    public async Task WhenTheDeliveryWriteFailsNoEventIsStaged()
    {
        var context = fixture.CreateIsolatedContext();
        await EnsureIndexesAsync(context);
        var unitOfWork = new MongoDeliveryUnitOfWork(fixture.Client, context, TimeProvider.System);

        var orderId = Guid.NewGuid();
        var firstDelivery = ScheduledDelivery(orderId, Guid.NewGuid());
        await unitOfWork.PersistScheduledDeliveryAsync(firstDelivery, ScheduledEvent(firstDelivery), CancellationToken.None);

        // A second delivery for the same order violates the unique OrderId index, so the delivery insert
        // (the first write in the transaction) fails and the event write must never happen.
        var duplicateDelivery = ScheduledDelivery(orderId, Guid.NewGuid());
        var persistDuplicate = async () => await unitOfWork.PersistScheduledDeliveryAsync(
            duplicateDelivery, ScheduledEvent(duplicateDelivery), CancellationToken.None);

        await persistDuplicate.Should().ThrowAsync<MongoException>();

        var stagedMessages = await context.Outbox.Find(FilterDefinition<OutboxMessage>.Empty).ToListAsync();
        stagedMessages.Should().ContainSingle("the failed delivery write must stage no event of its own");
    }

    [Fact]
    public async Task WhenTheEventWriteFailsTheDeliveryIsRolledBack()
    {
        var context = fixture.CreateIsolatedContext();

        // Force the second write in the transaction (the outbox insert) to fail deterministically: make
        // OccurredOnUtc unique and pin the clock so two staged events collide. The delivery write — the
        // first operation — must then roll back with the transaction, proving both-or-neither atomicity.
        await context.Outbox.Indexes.CreateOneAsync(new CreateIndexModel<OutboxMessage>(
            Builders<OutboxMessage>.IndexKeys.Ascending(message => message.OccurredOnUtc),
            new CreateIndexOptions { Unique = true }));
        var unitOfWork = new MongoDeliveryUnitOfWork(fixture.Client, context, new FixedTimeProvider(SlotStart));

        var firstDelivery = ScheduledDelivery(Guid.NewGuid(), Guid.NewGuid());
        await unitOfWork.PersistScheduledDeliveryAsync(firstDelivery, ScheduledEvent(firstDelivery), CancellationToken.None);

        var secondDelivery = ScheduledDelivery(Guid.NewGuid(), Guid.NewGuid());
        var persistSecond = async () => await unitOfWork.PersistScheduledDeliveryAsync(
            secondDelivery, ScheduledEvent(secondDelivery), CancellationToken.None);

        await persistSecond.Should().ThrowAsync<MongoException>();

        var rolledBack = await new MongoDeliveryRepository(context)
            .FindByOrderIdAsync(secondDelivery.OrderId, CancellationToken.None);
        rolledBack.Should().BeNull("the delivery write must roll back when the event write fails in the same transaction");
    }

    private static Task EnsureIndexesAsync(DeliveryMongoContext context) =>
        new DeliveryMongoIndexInitializer(context, NullLogger<DeliveryMongoIndexInitializer>.Instance)
            .StartAsync(CancellationToken.None);

    private static DeliveryAggregate ScheduledDelivery(Guid orderId, Guid customerId) => DeliveryAggregate.Schedule(
        orderId,
        customerId,
        new DeliveryAddress("5 Baker Street", "Suite 1", "London", "NW1 6XE", "GB"),
        SlotStart,
        SlotStart.AddHours(3),
        Guid.NewGuid(),
        SlotStart.AddDays(-1));

    private static DeliveryScheduledIntegrationEvent ScheduledEvent(DeliveryAggregate delivery) => new()
    {
        OrderId = delivery.OrderId,
        DeliveryId = delivery.Id,
        CustomerId = delivery.CustomerId,
        SlotStartUtc = delivery.SlotStartUtc,
        SlotEndUtc = delivery.SlotEndUtc,
    };
}
