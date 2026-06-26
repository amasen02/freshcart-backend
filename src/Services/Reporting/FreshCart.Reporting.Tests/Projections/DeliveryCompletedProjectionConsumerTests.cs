using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Reporting.Application.Common.Abstractions;
using FreshCart.Reporting.Application.Projections.Consumers;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FreshCart.Reporting.Tests.Projections;

public sealed class DeliveryCompletedProjectionConsumerTests
{
    private readonly IProjectionWriter projectionWriter = Substitute.For<IProjectionWriter>();
    private readonly DeliveryCompletedProjectionConsumer consumer;

    public DeliveryCompletedProjectionConsumerTests() =>
        consumer = new DeliveryCompletedProjectionConsumer(
            projectionWriter,
            NullLogger<DeliveryCompletedProjectionConsumer>.Instance);

    [Fact]
    public async Task AppliesProjectionOnFirstDelivery()
    {
        var integrationEvent = CreateCompletedEvent();
        projectionWriter.ApplyDeliveryCompletedAsync(integrationEvent, Arg.Any<CancellationToken>()).Returns(true);

        await consumer.Consume(CreateConsumeContext(integrationEvent));

        await projectionWriter.Received(1).ApplyDeliveryCompletedAsync(integrationEvent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TreatsAnAlreadyProcessedEventAsANoOp()
    {
        var integrationEvent = CreateCompletedEvent();
        projectionWriter.ApplyDeliveryCompletedAsync(integrationEvent, Arg.Any<CancellationToken>()).Returns(false);

        await consumer.Consume(CreateConsumeContext(integrationEvent));

        await projectionWriter.Received(1).ApplyDeliveryCompletedAsync(integrationEvent, Arg.Any<CancellationToken>());
    }

    private static ConsumeContext<DeliveryCompletedIntegrationEvent> CreateConsumeContext(DeliveryCompletedIntegrationEvent integrationEvent)
    {
        var consumeContext = Substitute.For<ConsumeContext<DeliveryCompletedIntegrationEvent>>();
        consumeContext.Message.Returns(integrationEvent);
        consumeContext.CancellationToken.Returns(CancellationToken.None);
        return consumeContext;
    }

    private static DeliveryCompletedIntegrationEvent CreateCompletedEvent() => new()
    {
        OrderId = Guid.NewGuid(),
        DeliveryId = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        DeliveredOnUtc = new DateTimeOffset(2026, 6, 26, 11, 0, 0, TimeSpan.Zero),
    };
}
