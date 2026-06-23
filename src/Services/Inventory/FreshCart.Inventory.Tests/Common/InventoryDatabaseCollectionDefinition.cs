using Xunit;

namespace FreshCart.Inventory.Tests.Common;

[CollectionDefinition(InventoryDatabaseFixture.CollectionName)]
public sealed class InventoryDatabaseCollectionDefinition : ICollectionFixture<InventoryDatabaseFixture>;
