using Xunit;

namespace FreshCart.CustomerSupport.Tests.Support;

[CollectionDefinition(MongoFixture.CollectionName)]
public sealed class MongoCollectionDefinition : ICollectionFixture<MongoFixture>;
