using FreshCart.Delivery.Application.Fulfilment;
using FreshCart.Delivery.Application.Scheduling;
using FreshCart.Delivery.Application.Tracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FreshCart.Delivery.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the delivery application core. There is deliberately no MediatR pipeline here: in this
    /// hexagon the use cases are plain application services that orchestrate the ports directly, so a
    /// command/query bus would add a dispatch hop without removing any branching. Validation lives at
    /// the HTTP edge (route constraints and the domain invariants) where it belongs for this service.
    /// </summary>
    public static IServiceCollection AddDeliveryApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ScheduleDeliveryService>();
        services.AddScoped<CompleteDeliveryService>();
        services.AddScoped<DeliveryTrackingQueries>();

        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
