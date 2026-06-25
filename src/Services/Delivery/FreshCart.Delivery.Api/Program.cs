using System.Globalization;
using System.Text;
using Carter;
using FreshCart.BuildingBlocks.Exceptions.Handler;
using FreshCart.BuildingBlocks.Messaging.Outbox;
using FreshCart.BuildingBlocks.Security;
using FreshCart.Delivery.Api.Configuration;
using FreshCart.Delivery.Application;
using FreshCart.Delivery.Infrastructure;
using FreshCart.ServiceDefaults;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;

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
    .AddDeliveryApplication()
    .AddDeliveryInfrastructure(webApplicationBuilder.Configuration, webApplicationBuilder.Environment);

// The transactional outbox closes the dual-write gap on DeliveryScheduled/DeliveryCompleted: the events
// are staged with the delivery write and this background publisher drains them onto the bus. MassTransit
// registers IPublishEndpoint as scoped, so the singleton publisher is handed the bus itself (IBus).
webApplicationBuilder.Services.Configure<OutboxPublisherOptions>(
    webApplicationBuilder.Configuration.GetSection(OutboxPublisherOptions.SectionName));
webApplicationBuilder.Services.AddSingleton<IHostedService>(serviceProvider => new OutboxPublisher(
    serviceProvider.GetRequiredService<IServiceScopeFactory>(),
    serviceProvider.GetRequiredService<IBus>(),
    serviceProvider.GetRequiredService<IOptions<OutboxPublisherOptions>>(),
    serviceProvider.GetRequiredService<ILogger<OutboxPublisher>>()));

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
    authorizationOptions.AddPolicy("Customer", policyBuilder =>
        policyBuilder.RequireAuthenticatedUser().RequireRole("Customer"));

    authorizationOptions.AddPolicy("BackOfficeUser", policyBuilder =>
        policyBuilder.RequireAuthenticatedUser().RequireRole("Administrator", "SupportAgent", "Manager"));
});

webApplicationBuilder.Services.AddDeliveryHealthChecks(webApplicationBuilder.Configuration);

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
