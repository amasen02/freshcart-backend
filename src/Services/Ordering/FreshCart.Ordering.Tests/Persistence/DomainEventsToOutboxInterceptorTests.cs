using FluentAssertions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Ordering.Domain.Orders;
using FreshCart.Ordering.Infrastructure.Persistence;
using FreshCart.Ordering.Infrastructure.Persistence.Interceptors;
using FreshCart.Ordering.Tests.Support;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FreshCart.Ordering.Tests.Persistence;

/// <summary>
/// Exercises the outbox interceptor end to end against a real EF Core model on SQLite. The assertion
/// that matters is structural: each domain transition produces exactly one correctly typed outbox row
/// inside the same SaveChanges, never zero and never two. Each test owns its own in-memory database so
/// the cases stay independent.
/// </summary>
public sealed class DomainEventsToOutboxInterceptorTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SubmittingAnOrderWritesASingleOrderPlacedOutboxRow()
    {
        await using var database = await OrderingTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();

        dbContext.Orders.Add(OrderTestData.SubmittedOrder());
        await dbContext.SaveChangesAsync();

        var outboxRows = await dbContext.OutboxMessages.ToListAsync();
        outboxRows.Should().ContainSingle()
            .Which.EventType.Should().Contain(nameof(OrderPlacedIntegrationEvent));
    }

    [Fact]
    public async Task ConfirmingAnOrderWritesAnOrderConfirmedRowCarryingTheLines()
    {
        await using var database = await OrderingTestDatabase.CreateAsync();
        var orderId = Guid.NewGuid();
        await database.PersistSubmittedOrderAsync(orderId);

        await using (var dbContext = database.CreateContext())
        {
            var order = await dbContext.Orders.SingleAsync(persistedOrder => persistedOrder.Id == orderId);
            order.MarkStockReserved(Guid.NewGuid());
            order.MarkPaid(Guid.NewGuid());
            order.Confirm(Now);
            await dbContext.SaveChangesAsync();
        }

        await using var assertionContext = database.CreateContext();
        var confirmedRow = await assertionContext.OutboxMessages
            .SingleAsync(message => message.EventType.Contains(nameof(OrderConfirmedIntegrationEvent)));
        confirmedRow.ContentJson.Should().Contain("SKU-APPLES-1KG");
    }

    [Fact]
    public async Task CancellingAnOrderWritesAnOrderCancelledRow()
    {
        await using var database = await OrderingTestDatabase.CreateAsync();
        var orderId = Guid.NewGuid();
        await database.PersistSubmittedOrderAsync(orderId);

        await using (var dbContext = database.CreateContext())
        {
            var order = await dbContext.Orders.SingleAsync(persistedOrder => persistedOrder.Id == orderId);
            order.Cancel("No longer needed", Now);
            await dbContext.SaveChangesAsync();
        }

        await using var assertionContext = database.CreateContext();
        var cancelledRows = await assertionContext.OutboxMessages
            .Where(message => message.EventType.Contains(nameof(OrderCancelledIntegrationEvent)))
            .ToListAsync();
        cancelledRows.Should().ContainSingle();
    }

    [Fact]
    public async Task RefundingAnOrderWritesAnOrderRefundedRow()
    {
        await using var database = await OrderingTestDatabase.CreateAsync();
        var orderId = Guid.NewGuid();
        await database.PersistSubmittedOrderAsync(orderId);

        await using (var dbContext = database.CreateContext())
        {
            var order = await dbContext.Orders.SingleAsync(persistedOrder => persistedOrder.Id == orderId);
            order.MarkStockReserved(Guid.NewGuid());
            order.MarkPaid(Guid.NewGuid());
            order.Confirm(Now);
            order.Refund("Damaged on delivery", Now);
            await dbContext.SaveChangesAsync();
        }

        await using var assertionContext = database.CreateContext();
        var refundedRows = await assertionContext.OutboxMessages
            .Where(message => message.EventType.Contains(nameof(OrderRefundedIntegrationEvent)))
            .ToListAsync();
        refundedRows.Should().ContainSingle();
    }

    private sealed class OrderingTestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private OrderingTestDatabase(SqliteConnection connection) => this.connection = connection;

        public static async Task<OrderingTestDatabase> CreateAsync()
        {
            var sqliteConnection = new SqliteConnection("DataSource=:memory:");
            await sqliteConnection.OpenAsync();

            var database = new OrderingTestDatabase(sqliteConnection);
            await using var dbContext = database.CreateContext();
            await dbContext.Database.EnsureCreatedAsync();
            return database;
        }

        public OrderingDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<OrderingDbContext>()
                .UseSqlite(connection)
                .AddInterceptors(new DomainEventsToOutboxInterceptor(new FixedTimeProvider(Now)))
                .Options;

            return new OrderingDbContext(options);
        }

        public async Task PersistSubmittedOrderAsync(Guid orderId)
        {
            await using var dbContext = CreateContext();
            dbContext.Orders.Add(OrderTestData.SubmittedOrder(orderId));
            await dbContext.SaveChangesAsync();
        }

        public ValueTask DisposeAsync() => connection.DisposeAsync();
    }
}
