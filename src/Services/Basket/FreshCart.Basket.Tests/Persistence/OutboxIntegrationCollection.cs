using Xunit;

namespace FreshCart.Basket.Tests.Persistence;

/// <summary>
/// Binds the Marten outbox store tests to the shared <see cref="OutboxIntegrationFixture"/> so the
/// PostgreSQL container starts once for the whole suite rather than per test class.
/// </summary>
[CollectionDefinition(Name)]
public sealed class OutboxIntegrationCollection : ICollectionFixture<OutboxIntegrationFixture>
{
    public const string Name = "Basket outbox integration";
}
