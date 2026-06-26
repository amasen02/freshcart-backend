using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Reporting.Application.Common.Abstractions;
using FreshCart.Reporting.Application.Projections.Consumers;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FreshCart.Reporting.Tests.Projections;

public sealed class DeliveryScheduledProjectionConsumerTests
{
    private readonly IProjectionWriter projectionWriter = Substitute.For<IProjectionWriter>();
    private readonly DeliveryScheduledProjectionConsumer consumer;

    public DeliveryScheduledProjectionConsumerTests() =>
        consumer = new DeliveryScheduledProjectionConsumer(
            projectionWriter,
            NullLogger<DeliveryScheduledProjectionConsumer>.Instance);

    [Fact]
    public async Task AppliesProjectionOnFirstDelivery()
    {
        var integrationEvent = CreateScheduledEvent();
        projectionWriter.ApplyDeliveryScheduledAsync(integrationEvent, Arg.Any<CancellationToken>()).Returns(true);

        await consumer.Consume(CreateConsumeContext(integrationEvent));

        await projectionWriter.Received(1).ApplyDeliveryScheduledAsync(integrationEvent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TreatsAnAlreadyProcessedEventAsANoOp()
    {
        var integrationEvent = CreateScheduledEvent();
        projectionWriter.ApplyDeliveryScheduledAsync(integrationEvent, Arg.Any<CancellationToken>()).Returns(false);

        await consumer.Consume(CreateConsumeContext(integrationEvent));

        await projectionWriter.Received(1).ApplyDeliveryScheduledAsync(integrationEvent, Arg.Any<CancellationToken>());
    }

    private static ConsumeContext<DeliveryScheduledIntegrationEvent> CreateConsumeContext(DeliveryScheduledIntegrationEvent integrationEvent)
    {
        var consumeContext = Substitute.For<ConsumeContext<DeliveryScheduledIntegrationEvent>>();
        consumeContext.Message.Returns(integrationEvent);
        consumeContext.CancellationToken.Returns(CancellationToken.None);
        return consumeContext;
    }

    private static DeliveryScheduledIntegrationEvent CreateScheduledEvent() => new()
    {
        OrderId = Guid.NewGuid(),
        DeliveryId = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        SlotStartUtc = new DateTimeOffset(2026, 6, 26, 9, 0, 0, TimeSpan.Zero),
        SlotEndUtc = new DateTimeOffset(2026, 6, 26, 12, 0, 0, TimeSpan.Zero),
    };
}
