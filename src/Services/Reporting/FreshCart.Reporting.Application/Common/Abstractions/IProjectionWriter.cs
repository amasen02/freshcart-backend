using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;

namespace FreshCart.Reporting.Application.Common.Abstractions;

/// <summary>
/// Applies a consumed integration event onto the materialised warehouse tables the dashboards read.
/// </summary>
public interface IProjectionWriter
{
    Task ApplyOrderConfirmedAsync(OrderConfirmedIntegrationEvent integrationEvent, CancellationToken cancellationToken);

    Task ApplyOrderRefundedAsync(OrderRefundedIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
