using Xunit;

namespace FreshCart.CustomerSupport.Tests.Support;

[CollectionDefinition(RedisFixture.CollectionName)]
public sealed class RedisCollectionDefinition : ICollectionFixture<RedisFixture>;
