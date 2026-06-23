namespace FreshCart.Delivery.Infrastructure.Persistence;

/// <summary>
/// Connection settings for the delivery MongoDB store. The connection string is the Aspire-supplied
/// <c>deliverydb</c> resource; the database name is parsed from it but can be overridden for tests.
/// </summary>
public sealed class DeliveryMongoOptions
{
    public const string ConnectionStringName = "deliverydb";

    public const string DefaultDatabaseName = "deliverydb";

    public required string ConnectionString { get; init; }

    public string DatabaseName { get; init; } = DefaultDatabaseName;
}
