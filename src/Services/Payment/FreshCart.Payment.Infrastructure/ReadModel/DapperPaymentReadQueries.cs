using Dapper;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Payment.Application.Abstractions;
using FreshCart.Payment.Application.Payments.Models;

namespace FreshCart.Payment.Infrastructure.ReadModel;

public sealed class DapperPaymentReadQueries(ISqlConnectionFactory connectionFactory)
    : IPaymentReadQueries
{
    private const string SelectPaymentColumns = """
        SELECT PaymentId, OrderId, CustomerId, Amount, RefundedAmount,
               CurrencyCode, Method, Status, ProviderReference, CreatedOnUtc, UpdatedOnUtc
        FROM dbo.payments
        """;

    private const string FindByOrderIdSql = SelectPaymentColumns + """

        WHERE OrderId = @OrderId;
        """;

    private const string GetPaymentsPageSql = """
        SELECT COUNT_BIG(*) FROM dbo.payments;

        """ + SelectPaymentColumns + """

        ORDER BY CreatedOnUtc DESC, PaymentId
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        """;

    public async Task<PaymentReadModel?> FindByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var connection = await connectionFactory
            .GetOpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        return await connection
            .QuerySingleOrDefaultAsync<PaymentReadModel>(
                new CommandDefinition(FindByOrderIdSql, new { OrderId = orderId }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    public async Task<PaginatedResult<PaymentReadModel>> GetPaymentsPageAsync(
        PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pagination);

        var connection = await connectionFactory
            .GetOpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        var pageParameters = new
        {
            Offset = (pagination.PageNumber - 1) * pagination.PageSize,
            pagination.PageSize,
        };

        var resultSets = await connection
            .QueryMultipleAsync(new CommandDefinition(GetPaymentsPageSql, pageParameters, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        await using (resultSets.ConfigureAwait(false))
        {
            var totalItemCount = await resultSets.ReadSingleAsync<long>().ConfigureAwait(false);
            var payments = await resultSets.ReadAsync<PaymentReadModel>().ConfigureAwait(false);

            return new PaginatedResult<PaymentReadModel>(
                pagination.PageNumber,
                pagination.PageSize,
                totalItemCount,
                payments.ToList());
        }
    }
}
