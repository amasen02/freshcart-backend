namespace FreshCart.Ordering.Tests.Persistence;

/// <summary>
/// Binds the EF outbox store tests to the shared <see cref="OutboxIntegrationFixture"/> so the SQL Server
/// container starts once for the whole suite rather than per test class.
/// </summary>
[CollectionDefinition(Name)]
public sealed class OutboxIntegrationCollection : ICollectionFixture<OutboxIntegrationFixture>
{
    public const string Name = "Ordering outbox integration";
}
