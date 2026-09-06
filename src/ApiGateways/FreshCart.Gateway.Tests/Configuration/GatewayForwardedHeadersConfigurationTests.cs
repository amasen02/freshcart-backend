using FreshCart.Gateway.Yarp.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FreshCart.Gateway.Tests.Configuration;

public sealed class GatewayForwardedHeadersConfigurationTests
{
    [Fact]
    public void TrustsNoForwardingHopWhenNoneIsConfigured()
    {
        var forwardedHeadersOptions = ResolveForwardedHeadersOptions(EmptyConfiguration());

        // Both lists empty is the fail-closed state: the pipeline leaves the middleware out entirely, so
        // Connection.RemoteIpAddress stays the real socket address and the rate-limit partition key with it.
        forwardedHeadersOptions.KnownProxies.Should().BeEmpty();
        forwardedHeadersOptions.KnownIPNetworks.Should().BeEmpty();
    }

    [Fact]
    public void TrustsOnlyTheConfiguredForwardingHops()
    {
        var forwardedHeadersOptions = ResolveForwardedHeadersOptions(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["ForwardedHeaders:KnownNetworks:0"] = "10.0.0.0/8",
            ["ForwardedHeaders:KnownProxies:0"] = "203.0.113.7",
        });

        forwardedHeadersOptions.KnownIPNetworks.Should().ContainSingle();
        forwardedHeadersOptions.KnownProxies.Should().ContainSingle();
    }

    [Fact]
    public void AcceptsASingleForwardedHopOnly()
    {
        var forwardedHeadersOptions = ResolveForwardedHeadersOptions(EmptyConfiguration());

        forwardedHeadersOptions.ForwardLimit.Should().Be(1);
    }

    private static Dictionary<string, string?> EmptyConfiguration() =>
        new(StringComparer.Ordinal);

    private static ForwardedHeadersOptions ResolveForwardedHeadersOptions(Dictionary<string, string?> configurationValues)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        var services = new ServiceCollection();
        services.AddGatewayForwardedHeaders(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
    }
}
