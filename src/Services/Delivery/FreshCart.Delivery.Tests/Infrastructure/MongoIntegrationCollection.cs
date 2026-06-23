namespace FreshCart.Delivery.Tests.Infrastructure;

/// <summary>
/// Binds every MongoDB repository test class to the shared <see cref="MongoIntegrationFixture"/> so the
/// container starts once for the whole suite rather than per test class.
/// </summary>
[CollectionDefinition(Name)]
public sealed class MongoIntegrationCollection : ICollectionFixture<MongoIntegrationFixture>
{
    public const string Name = "Delivery MongoDB integration";
}
