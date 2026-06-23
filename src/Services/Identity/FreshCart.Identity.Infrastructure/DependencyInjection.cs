using FreshCart.Identity.Application.Common.Abstractions;
using FreshCart.Identity.Domain.Roles;
using FreshCart.Identity.Domain.Users;
using FreshCart.Identity.Infrastructure.Cryptography;
using FreshCart.Identity.Infrastructure.Persistence;
using FreshCart.Identity.Infrastructure.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FreshCart.Identity.Infrastructure;

/// <summary>
/// Composition root for the infrastructure layer. Wires EF Core, ASP.NET Identity, the JWT issuer
/// and the refresh-token service. Each Application-layer abstraction has exactly one implementation
/// here.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var sqlServerConnectionString = configuration.GetConnectionString("identitydb")
            ?? throw new InvalidOperationException("Connection string \"identitydb\" is missing.");

        services.AddDbContext<IdentityDbContext>(databaseContextOptions =>
        {
            databaseContextOptions.UseSqlServer(sqlServerConnectionString, sqlServerOptions =>
            {
                sqlServerOptions.MigrationsHistoryTable("__EFMigrationsHistory", "identity");
                sqlServerOptions.EnableRetryOnFailure(maxRetryCount: 5);
            });
        });

        services
            .AddIdentityCore<ApplicationUser>(identityOptions =>
            {
                identityOptions.User.RequireUniqueEmail = true;

                identityOptions.Password.RequireDigit = true;
                identityOptions.Password.RequireLowercase = true;
                identityOptions.Password.RequireUppercase = true;
                identityOptions.Password.RequireNonAlphanumeric = true;
                identityOptions.Password.RequiredLength = 12;
                identityOptions.Password.RequiredUniqueChars = 4;

                identityOptions.Lockout.AllowedForNewUsers = true;
                identityOptions.Lockout.MaxFailedAccessAttempts = 5;
                identityOptions.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                identityOptions.SignIn.RequireConfirmedEmail = false;
                identityOptions.SignIn.RequireConfirmedAccount = false;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        // Replace the default PBKDF2 hasher with Argon2id.
        services.AddSingleton<IPasswordHasher<ApplicationUser>, Argon2PasswordHasher<ApplicationUser>>();

        services.Configure<JwtIssuerOptions>(configuration.GetSection(JwtIssuerOptions.SectionName));
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IIdentityAuditLog, EntityFrameworkAuditLog>();

        // Demo-account seeder. Internally guarded so it only runs when the host environment is
        // Development; see IdentityDataSeeder for the rationale.
        services.AddHostedService<IdentityDataSeeder>();

        return services;
    }
}
