using System.Globalization;
using FreshCart.BuildingBlocks.Exceptions.Handler;
using FreshCart.BuildingBlocks.Security;
using FreshCart.Gateway.Yarp.Auth;
using FreshCart.Gateway.Yarp.Configuration;
using FreshCart.Gateway.Yarp.Middleware;
using FreshCart.ServiceDefaults;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Serilog;
using Yarp.ReverseProxy.Transforms.Builder;

var webApplicationBuilder = WebApplication.CreateBuilder(args);

webApplicationBuilder.Host.UseSerilog((hostContext, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(hostContext.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProcessId()
        .Enrich.WithThreadId()
        .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);
});

webApplicationBuilder.AddFreshCartServiceDefaults();

webApplicationBuilder.Services.AddOptions<GatewayJwtOptions>()
    .Bind(webApplicationBuilder.Configuration.GetSection(GatewayJwtOptions.SectionName))
    .Validate(
        jwtOptions => !string.IsNullOrWhiteSpace(jwtOptions.SigningKey),
        "Jwt:SigningKey missing.")
    .ValidateOnStart();

webApplicationBuilder.Services.AddMemoryCache();
webApplicationBuilder.Services.AddSingleton<IDownstreamTokenSigner, HmacDownstreamTokenSigner>();
webApplicationBuilder.Services.AddSingleton<CookieToJwtTokenExchanger>();
webApplicationBuilder.Services.TryAddSingletonTimeProvider();

webApplicationBuilder.Services.AddExceptionHandler<CustomExceptionHandler>();
webApplicationBuilder.Services.AddProblemDetails();

webApplicationBuilder.Services.AddGatewayCookieAuthentication();
webApplicationBuilder.Services.AddGatewayAntiforgery();
webApplicationBuilder.Services.AddGatewayCors(webApplicationBuilder.Configuration);
webApplicationBuilder.Services.AddGatewayRateLimiting();

await webApplicationBuilder.Services
    .AddGatewaySharedDataProtectionAsync(webApplicationBuilder.Configuration)
    .ConfigureAwait(false);

webApplicationBuilder.Services.Configure<ForwardedHeadersOptions>(forwardedHeadersOptions =>
{
    forwardedHeadersOptions.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // In AKS the ingress controller is the only trusted hop that sets the forwarded headers; locally
    // the SPA connects directly. Clearing these lists lets the platform proxy populate the headers
    // without the gateway second-guessing the source network.
    forwardedHeadersOptions.KnownIPNetworks.Clear();
    forwardedHeadersOptions.KnownProxies.Clear();
});

webApplicationBuilder.Services
    .AddReverseProxy()
    .LoadFromConfig(webApplicationBuilder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms<TokenExchangeTransformProvider>();

webApplicationBuilder.Services.AddGatewayHealthChecks(webApplicationBuilder.Configuration);

var application = webApplicationBuilder.Build();

application.UseForwardedHeaders();
application.UseSerilogRequestLogging();
application.UseFreshCartSecurityHeaders();
application.UseExceptionHandler();
application.UseCors(GatewayCorsConfiguration.SpaPolicyName);
application.UseRateLimiter();
application.UseAuthentication();
application.UseAuthorization();
application.UseMiddleware<AntiforgeryValidationMiddleware>();

application.MapReverseProxy();
application.MapDefaultHealthEndpoints();

await application.RunAsync().ConfigureAwait(false);

/// <summary>
/// Surface for <c>WebApplicationFactory&lt;Program&gt;</c>-based integration tests.
/// </summary>
public partial class Program
{
    protected Program()
    {
    }
}
