using System.Globalization;
using Carter;
using FreshCart.BuildingBlocks.Exceptions.Handler;
using FreshCart.BuildingBlocks.Security;
using FreshCart.Identity.Api.Configuration;
using FreshCart.Identity.Application;
using FreshCart.Identity.Application.Common.Abstractions;
using FreshCart.Identity.Infrastructure;
using FreshCart.ServiceDefaults;
using Microsoft.AspNetCore.DataProtection;
using Serilog;
using StackExchange.Redis;

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

webApplicationBuilder.Services
    .AddIdentityApplication()
    .AddIdentityInfrastructure(webApplicationBuilder.Configuration);

webApplicationBuilder.Services.AddHttpContextAccessor();
webApplicationBuilder.Services.AddScoped<ICurrentRequestContext, HttpContextCurrentRequestContext>();

webApplicationBuilder.Services
    .AddFreshCartAuthentication(webApplicationBuilder.Configuration)
    .AddFreshCartAntiforgery();

// Data-protection keys persist to Redis so every replica and the gateway can decrypt the
// FreshCart.Session cookie. Integration tests run without a cache connection and fall back
// to the per-process key ring.
var cacheConnectionString = webApplicationBuilder.Configuration.GetConnectionString("cache");
if (!string.IsNullOrWhiteSpace(cacheConnectionString))
{
    const string dataProtectionApplicationName = "FreshCart.Identity";
    const string dataProtectionKeysRedisKey = "freshcart:dataprotection:keys";

    var redisConnectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(cacheConnectionString).ConfigureAwait(false);
    webApplicationBuilder.Services.AddSingleton<IConnectionMultiplexer>(redisConnectionMultiplexer);
    webApplicationBuilder.Services
        .AddDataProtection()
        .SetApplicationName(dataProtectionApplicationName)
        .PersistKeysToStackExchangeRedis(redisConnectionMultiplexer, dataProtectionKeysRedisKey);
}

webApplicationBuilder.Services.AddExceptionHandler<CustomExceptionHandler>();
webApplicationBuilder.Services.AddProblemDetails();

webApplicationBuilder.Services.AddCarter();
webApplicationBuilder.Services.AddEndpointsApiExplorer();
webApplicationBuilder.Services.AddOpenApi();

var corsOriginsSection = webApplicationBuilder.Configuration.GetSection("Cors:AllowedOrigins");
var allowedCorsOrigins = corsOriginsSection.Get<string[]>() ?? Array.Empty<string>();
webApplicationBuilder.Services.AddCors(corsOptions =>
{
    corsOptions.AddPolicy("FreshCartSpa", policyBuilder =>
    {
        policyBuilder
            .WithOrigins(allowedCorsOrigins)
            .AllowCredentials()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

webApplicationBuilder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: webApplicationBuilder.Configuration.GetConnectionString("identitydb")
            ?? throw new InvalidOperationException("identitydb connection string missing."),
        name: "identitydb",
        tags: ["ready"]);

var application = webApplicationBuilder.Build();

application.UseSerilogRequestLogging();
application.UseFreshCartSecurityHeaders();

if (application.Environment.IsDevelopment())
{
    application.MapOpenApi();
}
else
{
    application.UseHsts();
}

application.UseHttpsRedirection();
application.UseCors("FreshCartSpa");

application.UseAuthentication();
application.UseAuthorization();
application.UseAntiforgery();

application.UseExceptionHandler();
application.MapCarter();

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
