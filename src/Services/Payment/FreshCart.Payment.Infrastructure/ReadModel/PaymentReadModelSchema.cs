using Dapper;
using Microsoft.Data.SqlClient;

namespace FreshCart.Payment.Infrastructure.ReadModel;

/// <summary>
/// Owns the idempotent DDL for the payments read model. The script lives here as a constant
/// rather than a loose .sql file so the schema ships inside the assembly and integration tests
/// apply the exact same definition the service runs against.
/// </summary>
public static class PaymentReadModelSchema
{
    public const string ConnectionStringName = "paymentreaddb";

    private const string CreateSchemaScript = """
        IF OBJECT_ID(N'dbo.payments', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.payments (
                PaymentId         uniqueidentifier NOT NULL CONSTRAINT PK_payments PRIMARY KEY,
                OrderId           uniqueidentifier NOT NULL CONSTRAINT UX_payments_OrderId UNIQUE,
                CustomerId        uniqueidentifier NOT NULL,
                Amount            decimal(18,2)    NOT NULL CONSTRAINT CK_payments_AmountPositive CHECK (Amount > 0),
                RefundedAmount    decimal(18,2)    NOT NULL CONSTRAINT CK_payments_RefundedNonNegative CHECK (RefundedAmount >= 0),
                CurrencyCode      nchar(3)         NOT NULL,
                Method            nvarchar(50)     NOT NULL,
                Status            nvarchar(20)     NOT NULL,
                ProviderReference nvarchar(100)    NULL,
                CreatedOnUtc      datetimeoffset   NOT NULL,
                UpdatedOnUtc      datetimeoffset   NOT NULL,
                -- A CHECK that spans two columns must be table-level; SQL Server rejects it (error 8141)
                -- when declared inline on the RefundedAmount column.
                CONSTRAINT CK_payments_RefundedWithinAmount CHECK (RefundedAmount <= Amount)
            );
        END;
        """;

    public static Task EnsureCreatedAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return connection.ExecuteAsync(new CommandDefinition(CreateSchemaScript, cancellationToken: cancellationToken));
    }
}
