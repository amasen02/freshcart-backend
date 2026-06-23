using FreshCart.Delivery.Application.Abstractions;
using FreshCart.Delivery.Domain.Drivers;
using FreshCart.Delivery.Domain.Slots;
using FreshCart.Delivery.Domain.Zones;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FreshCart.Delivery.Infrastructure.Seeding;

/// <summary>
/// Seeds the Development zones, drivers and forward slot grid once, only when the store is empty. It
/// resolves the repositories from a scope because hosted services are singletons while the repositories
/// are scoped over the request-less Mongo client. Registered only in Development by the composition
/// root, and after the index initializer so the geospatial index exists before zones land.
/// </summary>
public sealed partial class DevelopmentDataSeeder(
    IServiceScopeFactory serviceScopeFactory,
    TimeProvider timeProvider,
    ILogger<DevelopmentDataSeeder> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var scope = serviceScopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            await SeedWithinScopeAsync(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SeedWithinScopeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var zoneRepository = serviceProvider.GetRequiredService<IZoneRepository>();
        var driverRepository = serviceProvider.GetRequiredService<IDriverRepository>();
        var slotRepository = serviceProvider.GetRequiredService<ISlotRepository>();

        var existingZones = await zoneRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        if (existingZones.Count > 0)
        {
            LogSeedSkipped();
            return;
        }

        var seededZones = await SeedZonesAsync(zoneRepository, cancellationToken).ConfigureAwait(false);
        await SeedDriversAsync(driverRepository, cancellationToken).ConfigureAwait(false);
        await SeedSlotsAsync(slotRepository, seededZones, cancellationToken).ConfigureAwait(false);

        LogSeedCompleted(seededZones.Count, DevelopmentSeedData.DriverNames.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<IReadOnlyList<DeliveryZone>> SeedZonesAsync(
        IZoneRepository zoneRepository,
        CancellationToken cancellationToken)
    {
        var seededZones = new List<DeliveryZone>(DevelopmentSeedData.Zones.Count);
        foreach (var definition in DevelopmentSeedData.Zones)
        {
            var zone = DeliveryZone.Create(definition.Name, definition.Polygon);
            await zoneRepository.AddAsync(zone, cancellationToken).ConfigureAwait(false);
            seededZones.Add(zone);
        }

        return seededZones;
    }

    private static async Task SeedDriversAsync(
        IDriverRepository driverRepository,
        CancellationToken cancellationToken)
    {
        foreach (var driverName in DevelopmentSeedData.DriverNames)
        {
            await driverRepository.AddAsync(Driver.Create(driverName), cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SeedSlotsAsync(
        ISlotRepository slotRepository,
        IReadOnlyList<DeliveryZone> zones,
        CancellationToken cancellationToken)
    {
        var firstDay = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        for (var dayOffset = 0; dayOffset < DevelopmentSeedData.SlotDaysAhead; dayOffset++)
        {
            var day = firstDay.AddDays(dayOffset);
            for (var slotIndex = 0; slotIndex < DevelopmentSeedData.SlotsPerDayPerZone; slotIndex++)
            {
                var startHour = DevelopmentSeedData.FirstSlotStartHourUtc
                    + (slotIndex * DevelopmentSeedData.SlotDurationHours);
                var startUtc = new DateTimeOffset(day.ToDateTime(new TimeOnly(startHour, minute: 0)), TimeSpan.Zero);
                var endUtc = startUtc.AddHours(DevelopmentSeedData.SlotDurationHours);

                foreach (var zone in zones)
                {
                    var slot = DeliverySlot.Create(zone.Id, startUtc, endUtc, DevelopmentSeedData.SlotCapacity);
                    await slotRepository.AddAsync(slot, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Delivery store already seeded; skipping Development seed")]
    private partial void LogSeedSkipped();

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Seeded {ZoneCount} delivery zones and {DriverCount} drivers with a 7-day slot grid")]
    private partial void LogSeedCompleted(int zoneCount, int driverCount);
}
