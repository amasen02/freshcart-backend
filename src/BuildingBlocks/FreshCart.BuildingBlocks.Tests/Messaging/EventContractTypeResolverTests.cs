using FluentAssertions;
using FreshCart.BuildingBlocks.Messaging.Events;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;

namespace FreshCart.BuildingBlocks.Tests.Messaging;

public sealed class EventContractTypeResolverTests
{
    [Fact]
    public void ResolvesTheStableFullNameContract()
    {
        var contractName = typeof(OrderConfirmedIntegrationEvent).FullName!;

        var resolved = EventContractTypeResolver.Resolve(contractName);

        resolved.Should().Be<OrderConfirmedIntegrationEvent>();
    }

    [Fact]
    public void StillResolvesALegacyAssemblyQualifiedNameSoInFlightOutboxRowsKeepPublishing()
    {
        var legacyContractName = typeof(OrderConfirmedIntegrationEvent).AssemblyQualifiedName!;

        var resolved = EventContractTypeResolver.Resolve(legacyContractName);

        resolved.Should().Be<OrderConfirmedIntegrationEvent>();
    }

    [Fact]
    public void ReturnsNullForAnUnknownContractName()
    {
        var resolved = EventContractTypeResolver.Resolve("FreshCart.Nonexistent.SomeEvent");

        resolved.Should().BeNull();
    }

    [Fact]
    public void IntegrationEventContractNameIsTheVersionIndependentFullName()
    {
        var integrationEvent = new SampleContractEvent();

        integrationEvent.EventType.Should().Be(typeof(SampleContractEvent).FullName);
        integrationEvent.EventType.Should().NotContain("Version=");
    }

    private sealed record SampleContractEvent : IntegrationEvent;
}
