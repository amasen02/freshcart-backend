using FreshCart.Gateway.Yarp.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FreshCart.Gateway.Tests.Configuration;

public sealed class GatewayRateLimitingTests
{
    [Fact]
    public void RegistersAGlobalLimiterCoveringEveryRequest()
    {
        var rateLimiterOptions = ResolveRateLimiterOptions();

        rateLimiterOptions.GlobalLimiter.Should().NotBeNull();
    }

    [Fact]
    public void RejectsThrottledCallersWithTooManyRequests()
    {
        var rateLimiterOptions = ResolveRateLimiterOptions();

        rateLimiterOptions.RejectionStatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
    }

    [Fact]
    public void ExposesTheStricterAuthenticationPolicyNameAsAConstantTheRoutesReference()
    {
        GatewayRateLimiting.AuthenticationPolicyName.Should().Be("auth");
    }

    private static RateLimiterOptions ResolveRateLimiterOptions()
    {
        var services = new ServiceCollection();
        services.AddGatewayRateLimiting();

        using var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;
    }
}
