using Dapper;
using Microsoft.Data.SqlClient;

namespace FreshCart.Inventory.Api.Persistence;

/// <summary>
/// Owns the idempotent DDL for the inventory tables. The script lives here as a constant rather
/// than a loose .sql file so the schema ships inside the assembly and integration tests apply the
/// exact same definition the service runs against.
/// </summary>
public static class InventorySchema
{
    public const string ConnectionStringName = "inventorydb";

    private const string CreateSchemaScript = """
        IF OBJECT_ID(N'dbo.stock_items', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.stock_items (
                ProductSku       nvarchar(50)   NOT NULL CONSTRAINT PK_stock_items PRIMARY KEY,
                ProductName      nvarchar(200)  NOT NULL,
                QuantityOnHand   int            NOT NULL CONSTRAINT CK_stock_items_OnHandNonNegative CHECK (QuantityOnHand >= 0),
                QuantityReserved int            NOT NULL CONSTRAINT CK_stock_items_ReservedNonNegative CHECK (QuantityReserved >= 0),
                UpdatedOnUtc     datetimeoffset NOT NULL,
                CONSTRAINT CK_stock_items_ReservedWithinOnHand CHECK (QuantityReserved <= QuantityOnHand)
            );
        END;

        IF OBJECT_ID(N'dbo.stock_reservations', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.stock_reservations (
                ReservationId uniqueidentifier NOT NULL CONSTRAINT PK_stock_reservations PRIMARY KEY,
                OrderId       uniqueidentifier NOT NULL,
                Status        nvarchar(20)     NOT NULL,
                CreatedOnUtc  datetimeoffset   NOT NULL,
                ReleasedOnUtc datetimeoffset   NULL,
                CONSTRAINT UX_stock_reservations_OrderId UNIQUE (OrderId)
            );
        END;

        IF OBJECT_ID(N'dbo.stock_reservation_lines', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.stock_reservation_lines (
                ReservationId uniqueidentifier NOT NULL CONSTRAINT FK_stock_reservation_lines_stock_reservations
                    REFERENCES dbo.stock_reservations (ReservationId),
                ProductSku    nvarchar(50)     NOT NULL,
                Quantity      int              NOT NULL,
                CONSTRAINT PK_stock_reservation_lines PRIMARY KEY (ReservationId, ProductSku)
            );
        END;
        """;

    public static Task EnsureCreatedAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return connection.ExecuteAsync(new CommandDefinition(CreateSchemaScript, cancellationToken: cancellationToken));
    }
}
