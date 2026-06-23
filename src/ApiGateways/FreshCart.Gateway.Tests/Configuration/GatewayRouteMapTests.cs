using Microsoft.Extensions.Configuration;

namespace FreshCart.Gateway.Tests.Configuration;

public sealed class GatewayRouteMapTests
{
    private const string ReverseProxyConfigurationFile = "GatewayConfiguration/appsettings.json";

    public static TheoryData<string, string, string?> PublishedRouteContract => new()
    {
        { "/api/auth/{**catch-all}", "identity", "/auth/{**catch-all}" },
        { "/api/account/{**catch-all}", "identity", "/account/{**catch-all}" },
        { "/api/products/{**catch-all}", "catalog", "/products/{**catch-all}" },
        { "/api/categories/{**catch-all}", "catalog", "/categories/{**catch-all}" },
        { "/api/brands/{**catch-all}", "catalog", "/brands/{**catch-all}" },
        { "/api/basket/{**catch-all}", "basket", "/basket/{**catch-all}" },
        { "/api/orders/{**catch-all}", "ordering", "/orders/{**catch-all}" },
        { "/api/delivery/{**catch-all}", "delivery", "/delivery/{**catch-all}" },
        { "/api/reviews/{**catch-all}", "reviews", "/reviews/{**catch-all}" },
        { "/api/inventory/{**catch-all}", "inventory", "/inventory/{**catch-all}" },
        { "/api/reporting/{**catch-all}", "reporting", "/{**catch-all}" },
        { "/hubs/notifications", "notification", null },
        { "/hubs/support", "customersupport", null },
    };

    public static TheoryData<string> AuthenticationRoutePaths => new()
    {
        "/api/auth/sign-in",
        "/api/auth/sign-up",
        "/api/auth/refresh",
    };

    [Theory]
    [MemberData(nameof(PublishedRouteContract))]
    public void EveryPublishedRouteMapsToTheContractedClusterAndTransform(
        string matchPath,
        string expectedCluster,
        string? expectedPathPattern)
    {
        var route = FindRouteByMatchPath(matchPath);

        route.GetValue<string>("ClusterId").Should().Be(expectedCluster);
        ReadFirstPathPattern(route).Should().Be(expectedPathPattern);
    }

    [Theory]
    [MemberData(nameof(AuthenticationRoutePaths))]
    public void TheSensitiveAuthenticationRoutesCarryTheStricterRateLimiterPolicy(string matchPath)
    {
        var route = FindRouteByMatchPath(matchPath);

        route.GetValue<string>("ClusterId").Should().Be("identity");
        route.GetValue<string>("RateLimiterPolicy").Should().Be("auth");
    }

    [Fact]
    public void EveryClusterReferencedByARouteIsDefined()
    {
        var configuration = LoadReverseProxyConfiguration();
        var routesSection = configuration.GetSection("ReverseProxy:Routes");
        var clustersSection = configuration.GetSection("ReverseProxy:Clusters");

        var definedClusterIds = clustersSection.GetChildren()
            .Select(cluster => cluster.Key)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var route in routesSection.GetChildren())
        {
            var clusterId = route.GetValue<string>("ClusterId");
            clusterId.Should().NotBeNullOrWhiteSpace();
            definedClusterIds.Should().Contain(clusterId!);
        }
    }

    private static IConfigurationSection FindRouteByMatchPath(string matchPath)
    {
        var configuration = LoadReverseProxyConfiguration();
        var route = configuration.GetSection("ReverseProxy:Routes")
            .GetChildren()
            .FirstOrDefault(candidate =>
                string.Equals(candidate.GetValue<string>("Match:Path"), matchPath, StringComparison.Ordinal));

        route.Should().NotBeNull($"the gateway must expose a route matching '{matchPath}'");
        return route!;
    }

    private static string? ReadFirstPathPattern(IConfigurationSection route) =>
        route.GetSection("Transforms")
            .GetChildren()
            .Select(transform => transform.GetValue<string>("PathPattern"))
            .FirstOrDefault(pathPattern => pathPattern is not null);

    private static IConfigurationRoot LoadReverseProxyConfiguration() =>
        new ConfigurationBuilder()
            .AddJsonFile(ReverseProxyConfigurationFile, optional: false)
            .Build();
}
