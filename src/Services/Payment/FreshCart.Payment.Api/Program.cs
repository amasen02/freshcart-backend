using System.Globalization;
using System.Text;
using Carter;
using FreshCart.BuildingBlocks.Exceptions.Handler;
using FreshCart.BuildingBlocks.Security;
using FreshCart.Payment.Api;
using FreshCart.Payment.Application;
using FreshCart.Payment.Infrastructure;
using FreshCart.Payment.Infrastructure.Projections;
using FreshCart.Payment.Infrastructure.ReadModel;
using FreshCart.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
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
    .AddPaymentApplication()
    .AddPaymentInfrastructure(webApplicationBuilder.Configuration);

// The SQL read model is projected asynchronously: each event append stages a projection intent in its
// Mongo transaction, and this background worker drains it into SQL idempotently.
webApplicationBuilder.Services.Configure<PaymentProjectionOptions>(
    webApplicationBuilder.Configuration.GetSection(PaymentProjectionOptions.SectionName));
webApplicationBuilder.Services.AddHostedService<PaymentReadModelProjectorService>();

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
    authorizationOptions.AddPolicy(AuthorizationPolicies.Administrator, policyBuilder => policyBuilder
        .RequireAuthenticatedUser()
        .RequireRole(AuthorizationPolicies.AdministratorRole));

    // Capture is only ever invoked service-to-service by the Ordering saga, never by a browser user.
    authorizationOptions.AddPolicy(ServiceAuthenticationDefaults.ServiceCallerPolicy, policyBuilder => policyBuilder
        .RequireAuthenticatedUser()
        .RequireRole(ServiceAuthenticationDefaults.ServiceAccountRole));
});

var paymentReadConnectionString = webApplicationBuilder.Configuration
    .GetConnectionString(PaymentReadModelSchema.ConnectionStringName)
    ?? throw new InvalidOperationException(
        $"Connection string \"{PaymentReadModelSchema.ConnectionStringName}\" missing.");

webApplicationBuilder.Services.AddHealthChecks()
    .AddSqlServer(paymentReadConnectionString, name: "paymentreaddb", tags: ["ready"])
    .AddMongoDb(
        serviceProvider => serviceProvider.GetRequiredService<IMongoClient>(),
        name: "paymentevents",
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
