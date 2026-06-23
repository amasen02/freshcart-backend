using System.Globalization;
using System.Text;
using Carter;
using FluentValidation;
using FreshCart.BuildingBlocks.Exceptions.Handler;
using FreshCart.BuildingBlocks.Messaging.MassTransit;
using FreshCart.BuildingBlocks.Security;
using FreshCart.Inventory.Api;
using FreshCart.Inventory.Api.Consumers;
using FreshCart.Inventory.Api.Grpc;
using FreshCart.Inventory.Api.Persistence;
using FreshCart.Inventory.Api.Repositories;
using FreshCart.Inventory.Api.Services;
using FreshCart.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
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

var inventoryConnectionString = webApplicationBuilder.Configuration.GetConnectionString(InventorySchema.ConnectionStringName)
    ?? throw new InvalidOperationException($"Connection string \"{InventorySchema.ConnectionStringName}\" missing.");

webApplicationBuilder.Services.AddScoped<ISqlConnectionFactory>(_ => new SqlConnectionFactory(inventoryConnectionString));
webApplicationBuilder.Services.AddScoped<IStockRepository, StockRepository>();
webApplicationBuilder.Services.AddScoped<IReservationRepository, ReservationRepository>();
webApplicationBuilder.Services.AddScoped<IStockReservationService, StockReservationService>();
webApplicationBuilder.Services.AddScoped<IStockLevelService, StockLevelService>();
webApplicationBuilder.Services.TryAddSingleton(TimeProvider.System);
webApplicationBuilder.Services.AddHostedService<InventorySchemaInitializer>();

webApplicationBuilder.Services.AddGrpc();
webApplicationBuilder.Services.AddCarter();
webApplicationBuilder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
webApplicationBuilder.Services.AddEndpointsApiExplorer();
webApplicationBuilder.Services.AddOpenApi();

webApplicationBuilder.Services.AddExceptionHandler<CustomExceptionHandler>();
webApplicationBuilder.Services.AddProblemDetails();

webApplicationBuilder.Services.AddRabbitMqMessageBroker(
    webApplicationBuilder.Configuration,
    typeof(ProductCreatedConsumer).Assembly);

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
    authorizationOptions.AddPolicy(AuthorizationPolicies.BackOfficeUser, policyBuilder => policyBuilder
        .RequireAuthenticatedUser()
        .RequireRole(
            AuthorizationPolicies.AdministratorRole,
            AuthorizationPolicies.SupportAgentRole,
            AuthorizationPolicies.ManagerRole));

    authorizationOptions.AddPolicy(AuthorizationPolicies.Administrator, policyBuilder => policyBuilder
        .RequireAuthenticatedUser()
        .RequireRole(AuthorizationPolicies.AdministratorRole));

    // The reserve/release gRPC surface is called only by the Ordering saga, never by a user.
    authorizationOptions.AddPolicy(ServiceAuthenticationDefaults.ServiceCallerPolicy, policyBuilder => policyBuilder
        .RequireAuthenticatedUser()
        .RequireRole(ServiceAuthenticationDefaults.ServiceAccountRole));
});

var messageBrokerOptions = webApplicationBuilder.Configuration
    .GetSection(MessageBrokerOptions.SectionName)
    .Get<MessageBrokerOptions>()
    ?? throw new InvalidOperationException("MessageBroker options missing.");

webApplicationBuilder.Services.AddHealthChecks()
    .AddSqlServer(inventoryConnectionString, name: "inventorydb", tags: ["ready"])
    .AddRabbitMQ(
        async _ =>
        {
            var rabbitConnectionFactory = new ConnectionFactory
            {
                Uri = new Uri(messageBrokerOptions.Host),
                UserName = messageBrokerOptions.UserName ?? ConnectionFactory.DefaultUser,
                Password = messageBrokerOptions.Password ?? ConnectionFactory.DefaultPass,
            };

            return await rabbitConnectionFactory.CreateConnectionAsync().ConfigureAwait(false);
        },
        name: "rabbitmq",
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
application.MapGrpcService<InventoryGrpcService>();

application.MapDefaultHealthEndpoints();

await application.RunAsync().ConfigureAwait(false);

/// <summary>Surface for integration tests that boot the host via <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program
{
    protected Program()
    {
    }
}
