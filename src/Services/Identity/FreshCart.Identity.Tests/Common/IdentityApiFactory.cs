using FreshCart.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;

namespace FreshCart.Identity.Tests.Common;

/// <summary>
/// Spins up a real SQL Server container per test class via Testcontainers, applies the schema, then
/// hosts the Identity Api in-process for end-to-end HTTP tests through <see cref="HttpClient"/>.
/// </summary>
/// <remarks>
/// Using a real database rather than EF in-memory is deliberate. The Identity service depends on
/// SQL Server semantics (collation, unique indexes, retry-on-failure) that the in-memory provider does
/// not reproduce. Mocks here would silently let production-breaking regressions through.
/// </remarks>
public sealed class IdentityApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestDatabaseName = "freshcart_identity_tests";

    private static readonly TimeSpan SqlServerReadinessTimeout = TimeSpan.FromSeconds(90);

    private static readonly TimeSpan SqlServerReadinessProbeInterval = TimeSpan.FromSeconds(2);

    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("FreshCart!IntegrationTest1")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ConnectionStrings:identitydb"] = BuildIdentityConnectionString(),
                // Blanks the Development cache connection so Data Protection stays on its default
                // per-process key ring. The test host has no Redis to connect to.
                ["ConnectionStrings:cache"] = string.Empty,
                ["Jwt:Issuer"] = "https://freshcart.test/identity",
                ["Jwt:Audience"] = "https://freshcart.test",
                ["Jwt:SigningKey"] = "integration-test-signing-key-please-replace-32chars",
            });
        });

        // The entry point bakes its connection string into singleton DbContext options before this
        // factory's configuration overlay is observable, so the registration is replaced outright
        // to guarantee the host targets the per-class container.
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<IdentityDbContext>>();
            services.AddDbContext<IdentityDbContext>(databaseContextOptions =>
                databaseContextOptions.UseSqlServer(BuildIdentityConnectionString(), sqlServerOptions =>
                {
                    sqlServerOptions.MigrationsHistoryTable("__EFMigrationsHistory", "identity");
                    sqlServerOptions.EnableRetryOnFailure(maxRetryCount: 5);
                }));
        });
    }

    public async Task InitializeAsync()
    {
        await _msSqlContainer.StartAsync().ConfigureAwait(false);
        await WaitUntilSqlServerAcceptsHostConnectionsAsync().ConfigureAwait(false);
        await CreateIdentitySchemaAsync().ConfigureAwait(false);
    }

    public new async Task DisposeAsync()
    {
        await _msSqlContainer.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    // The container's own wait strategy proves readiness from inside the container. The first
    // connection through the Docker port proxy can still stall, so the fixture also probes from
    // the host before any test runs.
    private async Task WaitUntilSqlServerAcceptsHostConnectionsAsync()
    {
        var readinessDeadlineUtc = DateTimeOffset.UtcNow + SqlServerReadinessTimeout;

        while (true)
        {
            try
            {
                var probeConnection = new SqlConnection(_msSqlContainer.GetConnectionString());
                await using (probeConnection.ConfigureAwait(false))
                {
                    await probeConnection.OpenAsync().ConfigureAwait(false);
                    return;
                }
            }
            catch (SqlException) when (DateTimeOffset.UtcNow < readinessDeadlineUtc)
            {
                await Task.Delay(SqlServerReadinessProbeInterval).ConfigureAwait(false);
            }
        }
    }

    // No EF migrations are checked in yet, so the service's startup migrator cannot build the
    // schema. The fixture materialises the model into a dedicated database instead. A dedicated
    // database matters because EnsureCreated skips table creation when the target already holds
    // tables, which the master database always does.
    private async Task CreateIdentitySchemaAsync()
    {
        var dbContextOptions = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlServer(BuildIdentityConnectionString())
            .Options;

        var identityDbContext = new IdentityDbContext(dbContextOptions);
        await using (identityDbContext.ConfigureAwait(false))
        {
            await identityDbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
        }
    }

    private string BuildIdentityConnectionString() =>
        new SqlConnectionStringBuilder(_msSqlContainer.GetConnectionString())
        {
            InitialCatalog = TestDatabaseName,
        }.ConnectionString;
}
