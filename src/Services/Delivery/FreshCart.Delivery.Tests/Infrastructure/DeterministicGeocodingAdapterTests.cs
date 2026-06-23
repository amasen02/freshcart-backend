using FluentAssertions;
using FreshCart.Delivery.Domain.Deliveries;
using FreshCart.Delivery.Infrastructure.Geocoding;

namespace FreshCart.Delivery.Tests.Infrastructure;

public sealed class DeterministicGeocodingAdapterTests
{
    private const double CityMinLatitude = 51.480;
    private const double CityMaxLatitude = 51.540;
    private const double CityMinLongitude = -0.150;
    private const double CityMaxLongitude = -0.040;

    private readonly DeterministicGeocodingAdapter adapter = new();

    [Fact]
    public void ResolvesTheSameCoordinateForTheSamePostcodeEveryTime()
    {
        var first = adapter.Locate(AddressWithPostcode("SW1A 1AA"));
        var second = adapter.Locate(AddressWithPostcode("SW1A 1AA"));

        second.Latitude.Should().Be(first.Latitude);
        second.Longitude.Should().Be(first.Longitude);
    }

    [Fact]
    public void IgnoresCasingAndWhitespaceWhenHashingThePostcode()
    {
        var canonical = adapter.Locate(AddressWithPostcode("SW1A 1AA"));
        var noisy = adapter.Locate(AddressWithPostcode("  sw1a1aa "));

        noisy.Latitude.Should().Be(canonical.Latitude);
        noisy.Longitude.Should().Be(canonical.Longitude);
    }

    [Theory]
    [InlineData("EC1A 1BB")]
    [InlineData("N1 9GU")]
    [InlineData("E1 6AN")]
    [InlineData("W1D 3QU")]
    public void AlwaysProducesACoordinateInsideTheSeededCityBounds(string postalCode)
    {
        var coordinate = adapter.Locate(AddressWithPostcode(postalCode));

        coordinate.Latitude.Should().BeInRange(CityMinLatitude, CityMaxLatitude);
        coordinate.Longitude.Should().BeInRange(CityMinLongitude, CityMaxLongitude);
    }

    [Fact]
    public void ResolvesDifferentCoordinatesForDifferentPostcodes()
    {
        var first = adapter.Locate(AddressWithPostcode("SW1A 1AA"));
        var second = adapter.Locate(AddressWithPostcode("E20 2ST"));

        second.Should().NotBe(first);
    }

    private static DeliveryAddress AddressWithPostcode(string postalCode) =>
        new("1 Test Road", null, "London", postalCode, "GB");
}
