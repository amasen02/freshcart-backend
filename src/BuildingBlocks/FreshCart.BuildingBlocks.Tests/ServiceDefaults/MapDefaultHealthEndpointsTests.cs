using System.Net;
using FluentAssertions;
using FreshCart.ServiceDefaults;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace FreshCart.BuildingBlocks.Tests.ServiceDefaults;

public sealed class MapDefaultHealthEndpointsTests
{
    private const string ReadinessTag = "ready";
    private const string DependencyCheckName = "dependency";

    private static readonly Uri AliveEndpoint = new("/alive", UriKind.Relative);
    private static readonly Uri ReadyEndpoint = new("/ready", UriKind.Relative);
    private static readonly Uri HealthEndpoint = new("/health", UriKind.Relative);

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task AliveAndReadyAreMappedInEveryEnvironmentSoKubernetesProbesNeverSeeNotFound(string environmentName)
    {
        await using var application = await StartHostAsync(environmentName, HealthCheckResult.Healthy());
        using var client = application.GetTestClient();

        var aliveResponse = await client.GetAsync(AliveEndpoint, CancellationToken.None);
        var readyResponse = await client.GetAsync(ReadyEndpoint, CancellationToken.None);

        aliveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        readyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthEndpointIsNotMappedInProductionBecauseItEnumeratesDependencyTopology()
    {
        await using var application = await StartHostAsync("Production", HealthCheckResult.Healthy());
        using var client = application.GetTestClient();

        var healthResponse = await client.GetAsync(HealthEndpoint, CancellationToken.None);

        healthResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    public async Task HealthEndpointReportsEveryRegisteredCheckOutsideProduction(string environmentName)
    {
        await using var application = await StartHostAsync(environmentName, HealthCheckResult.Healthy());
        using var client = application.GetTestClient();

        var healthResponse = await client.GetAsync(HealthEndpoint, CancellationToken.None);

        healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var healthPayload = await healthResponse.Content.ReadAsStringAsync(CancellationToken.None);
        healthPayload.Should().Contain(DependencyCheckName);
    }

    [Fact]
    public async Task LivenessStaysHealthyWhileAFailingDependencyTurnsReadinessUnhealthy()
    {
        await using var application = await StartHostAsync("Production", HealthCheckResult.Unhealthy("database unreachable"));
        using var client = application.GetTestClient();

        var aliveResponse = await client.GetAsync(AliveEndpoint, CancellationToken.None);
        var readyResponse = await client.GetAsync(ReadyEndpoint, CancellationToken.None);

        aliveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        readyResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    private static async Task<WebApplication> StartHostAsync(string environmentName, HealthCheckResult dependencyResult)
    {
        var webApplicationBuilder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environmentName });
        webApplicationBuilder.WebHost.UseTestServer();
        webApplicationBuilder.AddDefaultHealthChecks();
        webApplicationBuilder.Services
            .AddHealthChecks()
            .AddCheck(DependencyCheckName, () => dependencyResult, tags: [ReadinessTag]);

        var application = webApplicationBuilder.Build();
        application.MapDefaultHealthEndpoints();
        await application.StartAsync().ConfigureAwait(false);
        return application;
    }
}
