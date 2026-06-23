using Carter;
using FreshCart.BuildingBlocks.Exceptions.Handler;
using FreshCart.BuildingBlocks.Security;
using FreshCart.Reporting.Api.Authentication;
using FreshCart.Reporting.Application;
using FreshCart.Reporting.Infrastructure;
using FreshCart.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Text;
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
    .AddReportingApplication()
    .AddReportingInfrastructure(webApplicationBuilder.Configuration);

webApplicationBuilder.Services.AddCarter();
webApplicationBuilder.Services.AddEndpointsApiExplorer();
webApplicationBuilder.Services.AddOpenApi();

webApplicationBuilder.Services.AddExceptionHandler<CustomExceptionHandler>();
webApplicationBuilder.Services.AddProblemDetails();

// Authentication uses the JWT issued by the Identity service. Reporting is back-office only
// and the browser SPAs reach it through the gateway with the bearer scheme applied.
var jwtIssuer   = webApplicationBuilder.Configuration["Jwt:Issuer"]
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
    authorizationOptions.AddPolicy(AuthorizationPolicies.BackOfficeUser, policyBuilder =>
        policyBuilder
            .RequireAuthenticatedUser()
            .RequireRole(
                AuthorizationPolicies.AdministratorRoleName,
                AuthorizationPolicies.SupportAgentRoleName,
                AuthorizationPolicies.ManagerRoleName)));

webApplicationBuilder.Services.AddHealthChecks()
    .AddMySql(
        connectionString: webApplicationBuilder.Configuration.GetConnectionString("reportingdb")
            ?? throw new InvalidOperationException("reportingdb connection string missing."),
        name: "reportingdb",
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
application.UseAuthentication();
application.UseAuthorization();
application.UseExceptionHandler();

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
