using FreshCart.BuildingBlocks.Messaging.Outbox;
using FreshCart.Ordering.Domain.Orders;
using FreshCart.Ordering.Infrastructure.Persistence.Sagas;
using Microsoft.EntityFrameworkCore;

namespace FreshCart.Ordering.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Ordering write side. Holds the Order aggregate, the transactional outbox
/// and the MassTransit checkout saga state in one database so a single transaction commits the
/// aggregate change and the outbox rows together.
/// </summary>
public sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(OrderingSchema.Name);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderingDbContext).Assembly);

        ConfigureOrderConcurrencyToken(modelBuilder);

        new CheckoutStateMap().Configure(modelBuilder);
    }

    private void ConfigureOrderConcurrencyToken(ModelBuilder modelBuilder)
    {
        var rowVersion = modelBuilder.Entity<Order>().Property(order => order.RowVersion);

        if (Database.IsSqlServer())
        {
            // SQL Server rotates a native rowversion on every update, so the token is store-generated.
            rowVersion.IsRowVersion();
            return;
        }

        // Providers without a native rowversion (the SQLite used by the model tests) cannot generate
        // the value, so map it as a plain concurrency token with a non-null default. Production runs
        // on SQL Server; this branch only keeps the same model creatable elsewhere.
        rowVersion.IsConcurrencyToken().HasDefaultValue(Array.Empty<byte>());
    }
}
