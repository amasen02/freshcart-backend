using Xunit;

namespace FreshCart.Catalog.Tests.TestInfrastructure;

/// <summary>
/// Binds the Marten-backed integration tests to the shared <see cref="CatalogMartenFixture"/> so the
/// PostgreSQL container starts once for the whole suite rather than per test class.
/// </summary>
[CollectionDefinition(Name)]
public sealed class CatalogMartenCollection : ICollectionFixture<CatalogMartenFixture>
{
    public const string Name = "Catalog Marten integration";
}
