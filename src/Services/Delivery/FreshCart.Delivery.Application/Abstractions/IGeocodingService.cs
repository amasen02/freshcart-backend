using FreshCart.Delivery.Domain.Deliveries;
using FreshCart.Delivery.Domain.Zones;

namespace FreshCart.Delivery.Application.Abstractions;

/// <summary>
/// Port that resolves a postal address to a geographic coordinate. The domain reasons over coordinates
/// only; the concrete adapter (a deterministic local stand-in in DEV, a maps provider in production)
/// lives entirely outside the core and is swapped without touching any use case.
/// </summary>
public interface IGeocodingService
{
    GeoCoordinate Locate(DeliveryAddress address);
}
