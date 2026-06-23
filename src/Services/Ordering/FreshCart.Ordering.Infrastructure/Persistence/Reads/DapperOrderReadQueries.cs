using Dapper;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Ordering.Application.Abstractions;
using FreshCart.Ordering.Application.Orders.Dtos;

namespace FreshCart.Ordering.Infrastructure.Persistence.Reads;

/// <summary>
/// Dapper read projections for the order endpoints. These queries are hand-shaped for the HTTP
/// responses and never touch the aggregate or change tracking. Every parameter is bound, never
/// concatenated, so the read side stays injection-proof.
/// </summary>
public sealed class DapperOrderReadQueries(IOrderingConnectionFactory connectionFactory) : IOrderReadQueries
{
    private const string OrderDetailHeaderSql = """
        SELECT
            Id                   AS OrderId,
            CustomerId           AS CustomerId,
            Status               AS Status,
            CustomerEmail        AS CustomerEmail,
            CustomerDisplayName  AS CustomerDisplayName,
            PaymentMethod        AS PaymentMethod,
            SubtotalAmount       AS Subtotal,
            DiscountTotalAmount  AS DiscountTotal,
            TaxTotalAmount       AS TaxTotal,
            ShippingTotalAmount  AS ShippingTotal,
            GrandTotalAmount     AS GrandTotal,
            GrandTotalCurrency   AS CurrencyCode,
            FailureReason        AS FailureReason,
            SubmittedOnUtc       AS SubmittedOnUtc,
            ConfirmedOnUtc       AS ConfirmedOnUtc,
            CancelledOnUtc       AS CancelledOnUtc,
            BillingAddress_Line1      AS BillingLine1,
            BillingAddress_Line2      AS BillingLine2,
            BillingAddress_City       AS BillingCity,
            BillingAddress_PostalCode AS BillingPostalCode,
            BillingAddress_CountryCode AS BillingCountryCode,
            ShippingAddress_Line1      AS ShippingLine1,
            ShippingAddress_Line2      AS ShippingLine2,
            ShippingAddress_City       AS ShippingCity,
            ShippingAddress_PostalCode AS ShippingPostalCode,
            ShippingAddress_CountryCode AS ShippingCountryCode
        FROM ordering.Orders
        WHERE Id = @OrderId
        """;

    private const string OrderDetailLinesSql = """
        SELECT
            ProductId        AS ProductId,
            ProductSku       AS ProductSku,
            ProductName      AS ProductName,
            PrimaryCategory  AS PrimaryCategory,
            UnitPriceAmount  AS UnitPrice,
            Quantity         AS Quantity,
            IsDigital        AS IsDigital
        FROM ordering.OrderLines
        WHERE OrderId = @OrderId
        ORDER BY ProductName
        """;

    public async Task<PaginatedResult<OrderSummaryDto>> GetOrderSummariesPageAsync(
        Guid customerId,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(paginationRequest);

        var page = paginationRequest.Normalise();

        const string countSql = """
            SELECT COUNT(*)
            FROM ordering.Orders
            WHERE CustomerId = @CustomerId
            """;

        const string pageSql = """
            SELECT
                o.Id              AS OrderId,
                o.Status          AS Status,
                o.GrandTotalAmount   AS GrandTotal,
                o.GrandTotalCurrency AS CurrencyCode,
                (SELECT COUNT(*) FROM ordering.OrderLines l WHERE l.OrderId = o.Id) AS LineCount,
                o.SubmittedOnUtc  AS SubmittedOnUtc
            FROM ordering.Orders o
            WHERE o.CustomerId = @CustomerId
            ORDER BY o.SubmittedOnUtc DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var offset = (page.PageNumber - 1) * page.PageSize;

        var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false))
        {
            var totalItemCount = await connection
                .ExecuteScalarAsync<long>(new CommandDefinition(
                    countSql,
                    new { CustomerId = customerId },
                    cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            var summaries = await connection
                .QueryAsync<OrderSummaryDto>(new CommandDefinition(
                    pageSql,
                    new { CustomerId = customerId, Offset = offset, page.PageSize },
                    cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return new PaginatedResult<OrderSummaryDto>(
                page.PageNumber,
                page.PageSize,
                totalItemCount,
                [.. summaries]);
        }
    }

    public async Task<OrderDetailDto?> GetOrderDetailAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false))
        {
            var header = await connection
                .QuerySingleOrDefaultAsync<OrderDetailRow>(new CommandDefinition(
                    OrderDetailHeaderSql,
                    new { OrderId = orderId },
                    cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            if (header is null)
            {
                return null;
            }

            var lines = await connection
                .QueryAsync<OrderLineDto>(new CommandDefinition(
                    OrderDetailLinesSql,
                    new { OrderId = orderId },
                    cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return header.ToDto([.. lines]);
        }
    }
}
