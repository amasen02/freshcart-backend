using System.Globalization;
using System.Text;
using Carter;
using FreshCart.BuildingBlocks.Exceptions.Handler;
using FreshCart.BuildingBlocks.Messaging.Outbox;
using FreshCart.BuildingBlocks.Security;
using FreshCart.Ordering.Api.Authentication;
using FreshCart.Ordering.Api.Messaging;
using FreshCart.Ordering.Application;
using FreshCart.Ordering.Infrastructure;
using FreshCart.Ordering.Infrastructure.Persistence;
using FreshCart.ServiceDefaults;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var webApplicationBuilder = WebApplication.CreateBuilder(args);

webApplicationBuilder.Host.UseSerilog((hostContext, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(hostContext.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture));

webApplicationBuilder.AddFreshCartServiceDefaults();

webApplicationBuilder.Services
    .AddOrderingApplication()
    .AddOrderingInfrastructure(webApplicationBuilder.Configuration)
    .AddOrderingMessaging(webApplicationBuilder.Configuration);

// The outbox publisher is a root singleton; MassTransit registers IPublishEndpoint as scoped, so the
// publisher takes the bus itself, which implements the same interface.
webApplicationBuilder.Services.AddSingleton<IHostedService>(serviceProvider => new OutboxPublisher(
    serviceProvider.GetRequiredService<IServiceScopeFactory>(),
    serviceProvider.GetRequiredService<IBus>(),
    serviceProvider.GetRequiredService<IOptions<OutboxPublisherOptions>>(),
    serviceProvider.GetRequiredService<ILogger<OutboxPublisher>>()));

if (webApplicationBuilder.Environment.IsDevelopment())
{
    webApplicationBuilder.Services.AddHostedService<OrderingDatabaseInitializer>();
}

webApplicationBuilder.Services.AddCarter();
webApplicationBuilder.Services.AddEndpointsApiExplorer();
webApplicationBuilder.Services.AddOpenApi();

webApplicationBuilder.Services.AddExceptionHandler<CustomExceptionHandler>();
webApplicationBuilder.Services.AddProblemDetails();

var jwtIssuer = webApplicationBuilder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer missing.");
var jwtAudience = webApplicationBuilder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience missing.");
var jwtSigningKey = webApplicationBuilder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Jwt:SigningKey missing.");

webApplicationBuilder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, bearerOptions =>
    {
        bearerOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ClockSkew = FreshCartTokenDefaults.JwtClockSkew,
        };
    });

webApplicationBuilder.Services.AddAuthorization(authorizationOptions =>
{
    authorizationOptions.AddPolicy(OrderingAuthorizationPolicies.Customer, policyBuilder =>
        policyBuilder
            .RequireAuthenticatedUser()
            .RequireRole(OrderingAuthorizationPolicies.CustomerRoleName, OrderingAuthorizationPolicies.AdministratorRoleName));

    authorizationOptions.AddPolicy(OrderingAuthorizationPolicies.Administrator, policyBuilder =>
        policyBuilder
            .RequireAuthenticatedUser()
            .RequireRole(OrderingAuthorizationPolicies.AdministratorRoleName));
});

var orderingConnectionName = FreshCart.Ordering.Infrastructure.DependencyInjection.OrderingConnectionName;
webApplicationBuilder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: webApplicationBuilder.Configuration.GetConnectionString(orderingConnectionName)
            ?? throw new InvalidOperationException("orderingdb connection string missing."),
        name: orderingConnectionName,
        tags: ["ready"]);

var application = webApplicationBuilder.Build();

application.UseSerilogRequestLogging();
application.UseFreshCartSecurityHeaders();
application.UseExceptionHandler();

if (application.Environment.IsDevelopment())
{
    application.MapOpenApi();
}
else
{
    application.UseHsts();
}

application.UseHttpsRedirection();
application.UseAuthentication();
application.UseAuthorization();

application.MapCarter();

application.MapDefaultHealthEndpoints();

await application.RunAsync().ConfigureAwait(false);

/// <summary>Surface for integration tests that boot the host via <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program
{
    protected Program()
    {
    }
}
