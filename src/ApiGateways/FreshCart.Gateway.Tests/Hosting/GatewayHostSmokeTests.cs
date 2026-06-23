using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace FreshCart.Gateway.Tests.Hosting;

public sealed class GatewayHostSmokeTests : IClassFixture<GatewayHostSmokeTests.GatewayApplicationFactory>
{
    private readonly GatewayApplicationFactory applicationFactory;

    public GatewayHostSmokeTests(GatewayApplicationFactory applicationFactory) =>
        this.applicationFactory = applicationFactory;

    [Fact]
    public async Task TheHostBootsAndAnswersTheLivenessProbe()
    {
        using var httpClient = applicationFactory.CreateClient();

        using var response = await httpClient.GetAsync(new Uri("/alive", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnknownPathsAreNotProxiedAndReturnNotFound()
    {
        using var httpClient = applicationFactory.CreateClient();

        using var response = await httpClient.GetAsync(new Uri("/nothing-here", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public sealed class GatewayApplicationFactory : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            // A neutral environment skips the Development overrides that point data protection and the
            // readiness check at a real Redis instance, so the host boots without external dependencies.
            builder.UseEnvironment("IntegrationTest");

            builder.ConfigureAppConfiguration(configurationBuilder =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["Jwt:SigningKey"] = "gateway-integration-test-signing-key-please-replace-32+chars",
                });
            });

            return base.CreateHost(builder);
        }
    }
}
