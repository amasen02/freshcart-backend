using Xunit;

namespace FreshCart.Reviews.Tests.TestInfrastructure;

[CollectionDefinition(MongoFixture.CollectionName)]
public sealed class MongoCollectionDefinition : ICollectionFixture<MongoFixture>;
