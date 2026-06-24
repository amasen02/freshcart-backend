using FreshCart.Identity.Domain.Roles;
using FreshCart.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FreshCart.Identity.Infrastructure.Persistence;

/// <summary>
/// Seeds canonical roles and three demo accounts on first start when the host environment is
/// Development. Guarded so it never writes to Staging or Production. The credentials it creates
/// are the ones advertised on the landing page so a reviewer can sign in without any setup.
/// </summary>
public sealed class IdentityDataSeeder(
    IServiceScopeFactory serviceScopeFactory,
    IHostEnvironment hostEnvironment,
    ILogger<IdentityDataSeeder> logger) : IHostedService
{
    private static readonly IReadOnlyList<string> CanonicalRoleNames =
    [
        CanonicalRoles.Customer,
        CanonicalRoles.SupportAgent,
        CanonicalRoles.Administrator,
    ];

    private static readonly IReadOnlyList<DemoAccount> DemoAccounts =
    [
        new("demo@freshcart.test",    "Demo Customer",      "Demo-P@ssw0rd-2026",    CanonicalRoles.Customer),
        new("support@freshcart.test", "Demo Support Agent", "Support-P@ssw0rd-2026", CanonicalRoles.SupportAgent),
        new("admin@freshcart.test",   "Demo Administrator", "Admin-P@ssw0rd-2026",   CanonicalRoles.Administrator),
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!hostEnvironment.IsDevelopment())
        {
            logger.LogInformation(
                "IdentityDataSeeder skipped because environment is \"{EnvironmentName}\" (Development required).",
                hostEnvironment.EnvironmentName);
            return;
        }

        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        // EnsureCreated, not Migrate, is the developer-loop schema path used across the platform
        // (cf. OrderingDatabaseInitializer): the Identity service ships no EF migrations, so MigrateAsync
        // would create an empty database with no tables and the role seeding below would then fail with
        // "Invalid object name". Production schema management via generated migrations is a separate item.
        await dbContext.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        await EnsureCanonicalRolesExistAsync(roleManager).ConfigureAwait(false);

        foreach (var account in DemoAccounts)
        {
            await EnsureDemoAccountAsync(userManager, account).ConfigureAwait(false);
        }

        logger.LogInformation(
            "IdentityDataSeeder completed. {AccountCount} demo accounts available in Development.",
            DemoAccounts.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureCanonicalRolesExistAsync(RoleManager<ApplicationRole> roleManager)
    {
        foreach (var roleName in CanonicalRoleNames)
        {
            if (await roleManager.RoleExistsAsync(roleName).ConfigureAwait(false))
            {
                continue;
            }

            var result = await roleManager
                .CreateAsync(new ApplicationRole(roleName) { Description = $"Canonical \"{roleName}\" role." })
                .ConfigureAwait(false);

            if (!result.Succeeded)
            {
                logger.LogError(
                    "Failed to create canonical role {RoleName}: {Errors}",
                    roleName,
                    string.Join(", ", result.Errors.Select(error => error.Description)));
            }
        }
    }

    private async Task EnsureDemoAccountAsync(UserManager<ApplicationUser> userManager, DemoAccount account)
    {
        if (await userManager.FindByEmailAsync(account.Email).ConfigureAwait(false) is not null)
        {
            logger.LogDebug("Demo account {Email} already exists; skipping.", account.Email);
            return;
        }

        var user = new ApplicationUser
        {
            UserName = account.Email,
            Email = account.Email,
            EmailConfirmed = true,
            DisplayName = account.DisplayName,
            MarketingConsent = false,
        };

        var createResult = await userManager.CreateAsync(user, account.Password).ConfigureAwait(false);
        if (!createResult.Succeeded)
        {
            logger.LogError(
                "Failed to create demo account {Email}: {Errors}",
                account.Email,
                string.Join(", ", createResult.Errors.Select(error => error.Description)));
            return;
        }

        var assignResult = await userManager.AddToRoleAsync(user, account.RoleName).ConfigureAwait(false);
        if (!assignResult.Succeeded)
        {
            logger.LogError(
                "Failed to assign role {RoleName} to demo account {Email}: {Errors}",
                account.RoleName,
                account.Email,
                string.Join(", ", assignResult.Errors.Select(error => error.Description)));
            return;
        }

        logger.LogInformation("Seeded demo account {Email} ({RoleName}).", account.Email, account.RoleName);
    }

    private sealed record DemoAccount(string Email, string DisplayName, string Password, string RoleName);
}
