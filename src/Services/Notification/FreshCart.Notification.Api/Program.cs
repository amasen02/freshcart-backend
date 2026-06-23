using System.Globalization;
using Carter;
using FreshCart.BuildingBlocks.Exceptions.Handler;
using FreshCart.BuildingBlocks.Messaging.MassTransit;
using FreshCart.BuildingBlocks.Security;
using FreshCart.Notification.Api.Configuration;
using FreshCart.Notification.Api.Consumers;
using FreshCart.Notification.Api.Hubs;
using FreshCart.ServiceDefaults;
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

webApplicationBuilder.Services.AddNotificationServices(webApplicationBuilder.Configuration);
webApplicationBuilder.Services.AddNotificationJwtAuthentication(webApplicationBuilder.Configuration);
webApplicationBuilder.Services.AddRabbitMqMessageBroker(
    webApplicationBuilder.Configuration,
    typeof(OrderPlacedConsumer).Assembly);

webApplicationBuilder.Services.AddCarter();
webApplicationBuilder.Services.AddEndpointsApiExplorer();
webApplicationBuilder.Services.AddOpenApi();

webApplicationBuilder.Services.AddExceptionHandler<CustomExceptionHandler>();
webApplicationBuilder.Services.AddProblemDetails();

webApplicationBuilder.Services.AddNotificationHealthChecks(webApplicationBuilder.Configuration);

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
application.MapHub<NotificationHub>("/hubs/notifications");

application.MapDefaultHealthEndpoints();

await application.RunAsync().ConfigureAwait(false);

/// <summary>Surface for integration tests that boot the host via <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program
{
    protected Program()
    {
    }
}
