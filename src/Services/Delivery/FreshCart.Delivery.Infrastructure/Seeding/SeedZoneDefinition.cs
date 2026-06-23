using FreshCart.Delivery.Domain.Zones;

namespace FreshCart.Delivery.Infrastructure.Seeding;

/// <summary>
/// A named zone polygon used by the Development seeder. Held as a small value so the seed data reads as
/// declarative geography rather than imperative document construction.
/// </summary>
internal sealed record SeedZoneDefinition(string Name, ZonePolygon Polygon);
