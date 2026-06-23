using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using Carter;
using FluentValidation;
using FreshCart.BuildingBlocks.Exceptions.Handler;
using FreshCart.BuildingBlocks.Security;
using FreshCart.Pricing.Grpc.Configuration;
using FreshCart.Pricing.Grpc.Persistence;
using FreshCart.Pricing.Grpc.Services;
using FreshCart.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
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

var pricingConnectionString = webApplicationBuilder.Configuration.GetConnectionString("pricingdb")
    ?? throw new InvalidOperationException("pricingdb connection string missing.");

webApplicationBuilder.Services.AddDbContext<PricingDbContext>(dbContextOptions =>
    dbContextOptions.UseSqlite(pricingConnectionString));

webApplicationBuilder.Services
    .AddOptions<PricingOptions>()
    .BindConfiguration(PricingOptions.SectionName);

webApplicationBuilder.Services.AddSingleton(TimeProvider.System);
webApplicationBuilder.Services.AddScoped<CouponValidator>();
webApplicationBuilder.Services.AddScoped<BasketPriceCalculator>();
webApplicationBuilder.Services.AddHostedService<PricingDatabaseInitializer>();

webApplicationBuilder.Services.AddGrpc();
webApplicationBuilder.Services.AddCarter();
webApplicationBuilder.Services.AddValidatorsFromAssemblyContaining<Program>();
webApplicationBuilder.Services.AddEndpointsApiExplorer();
webApplicationBuilder.Services.AddOpenApi();
webApplicationBuilder.Services.ConfigureHttpJsonOptions(jsonOptions =>
    jsonOptions.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

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
    authorizationOptions.AddPolicy(AuthorizationPolicyNames.Administrator, policyBuilder =>
        policyBuilder
            .RequireAuthenticatedUser()
            .RequireRole(AuthorizationPolicyNames.Administrator));
});

webApplicationBuilder.Services.AddHealthChecks()
    .AddCheck<SqliteDatabaseHealthCheck>("pricingdb", tags: ["ready"]);

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

// No UseHttpsRedirection here: Basket calls the gRPC endpoint over plain HTTP/2 inside the
// cluster, and a 307 redirect would fail every unary call before it reached the service.
application.UseAuthentication();
application.UseAuthorization();

application.MapGrpcService<PricingGrpcService>();
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
