using Xunit;

namespace FreshCart.Payment.Tests.Common;

[CollectionDefinition(MongoEventStoreFixture.CollectionName)]
public sealed class MongoEventStoreCollectionDefinition : ICollectionFixture<MongoEventStoreFixture>;
