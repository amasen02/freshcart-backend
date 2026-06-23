using System.Text;
using FreshCart.Identity.Infrastructure.Tokens;
using FreshCart.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace FreshCart.Identity.Api.Configuration;

/// <summary>
/// Configures the two authentication schemes the service supports:
/// <list type="bullet">
/// <item><description><c>Cookies</c>: HttpOnly, Secure, SameSite=Strict; used by the Angular SPA.</description></item>
/// <item><description><c>Bearer</c>: JWT; used by mobile clients and other microservices.</description></item>
/// </list>
/// The Cookies scheme is the default; bearer is opt-in per endpoint via the
/// <c>JwtBearerDefaults.AuthenticationScheme</c> policy.
/// </summary>
public static class AuthenticationConfiguration
{
    public const string SessionCookieName = "FreshCart.Session";

    private static readonly TimeSpan SessionAbsoluteLifetime = TimeSpan.FromHours(8);

    public static IServiceCollection AddFreshCartAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var jwtOptions = configuration.GetSection(JwtIssuerOptions.SectionName).Get<JwtIssuerOptions>()
            ?? throw new InvalidOperationException("Missing JWT configuration.");

        services
            .AddAuthentication(authenticationOptions =>
            {
                authenticationOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                authenticationOptions.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, ConfigureSessionCookie)
            .AddJwtBearer(
                JwtBearerDefaults.AuthenticationScheme,
                bearerOptions => ConfigureJwtBearer(bearerOptions, jwtOptions));

        services.AddAuthorization(AddRolePolicies);

        return services;
    }

    private static void ConfigureSessionCookie(CookieAuthenticationOptions cookieOptions)
    {
        cookieOptions.Cookie.Name = SessionCookieName;
        cookieOptions.Cookie.HttpOnly = true;
        cookieOptions.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        cookieOptions.Cookie.SameSite = SameSiteMode.Strict;
        cookieOptions.Cookie.IsEssential = true;
        cookieOptions.ExpireTimeSpan = SessionAbsoluteLifetime;
        cookieOptions.SlidingExpiration = true;

        cookieOptions.Events.OnRedirectToLogin = redirectContext =>
        {
            // APIs should answer with 401 rather than a redirect.
            redirectContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };

        cookieOptions.Events.OnRedirectToAccessDenied = redirectContext =>
        {
            redirectContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    }

    private static void ConfigureJwtBearer(JwtBearerOptions bearerOptions, JwtIssuerOptions jwtOptions)
    {
        bearerOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = FreshCartTokenDefaults.JwtClockSkew,
        };
    }

    private static void AddRolePolicies(AuthorizationOptions authorizationOptions)
    {
        authorizationOptions.AddPolicy(AuthorizationPolicies.Customer, policy =>
            policy.RequireAuthenticatedUser().RequireRole(AuthorizationPolicies.Customer));

        authorizationOptions.AddPolicy(AuthorizationPolicies.SupportAgent, policy =>
            policy.RequireAuthenticatedUser().RequireRole(AuthorizationPolicies.SupportAgent));

        authorizationOptions.AddPolicy(AuthorizationPolicies.Administrator, policy =>
            policy.RequireAuthenticatedUser().RequireRole(AuthorizationPolicies.Administrator));
    }
}
