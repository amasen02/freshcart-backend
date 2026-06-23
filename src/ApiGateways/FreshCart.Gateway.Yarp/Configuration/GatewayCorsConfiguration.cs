using Microsoft.Extensions.Configuration;

namespace FreshCart.Gateway.Yarp.Configuration;

/// <summary>
/// CORS policy for the Angular SPA. Mirrors the policy name and shape the Identity service uses so a
/// browser that authenticated against Identity can call every gateway route with credentials.
/// </summary>
public static class GatewayCorsConfiguration
{
    public const string SpaPolicyName = "FreshCartSpa";

    private const string AllowedOriginsConfigurationKey = "Cors:AllowedOrigins";

    public static IServiceCollection AddGatewayCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var allowedOrigins = configuration.GetSection(AllowedOriginsConfigurationKey).Get<string[]>()
            ?? [];

        services.AddCors(corsOptions =>
        {
            corsOptions.AddPolicy(SpaPolicyName, policyBuilder =>
            {
                policyBuilder
                    .WithOrigins(allowedOrigins)
                    .AllowCredentials()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}
