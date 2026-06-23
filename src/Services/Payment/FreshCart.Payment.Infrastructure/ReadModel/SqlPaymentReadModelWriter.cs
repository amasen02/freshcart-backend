using Dapper;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Payment.Application.Abstractions;
using FreshCart.Payment.Application.Payments.Models;
using Microsoft.Data.SqlClient;

namespace FreshCart.Payment.Infrastructure.ReadModel;

/// <summary>
/// Synchronous projector for the payments read model, invoked in the request path after every
/// event append. The unique index on OrderId is the cross-stream backstop: if two concurrent
/// captures for the same order both miss the idempotency lookup, the second projection fails here
/// instead of recording two payments for one order.
/// </summary>
public sealed class SqlPaymentReadModelWriter(ISqlConnectionFactory connectionFactory)
    : IPaymentReadModelWriter
{
    private const int UniqueIndexViolationErrorNumber = 2601;
    private const int UniqueConstraintViolationErrorNumber = 2627;

    private const string UpsertPaymentSql = """
        MERGE dbo.payments AS target
        USING (SELECT @PaymentId AS PaymentId) AS source
            ON target.PaymentId = source.PaymentId
        WHEN MATCHED THEN
            UPDATE SET
                RefundedAmount = @RefundedAmount,
                Status = @Status,
                ProviderReference = @ProviderReference,
                UpdatedOnUtc = @UpdatedOnUtc
        WHEN NOT MATCHED THEN
            INSERT (PaymentId, OrderId, CustomerId, Amount, RefundedAmount, CurrencyCode, Method, Status, ProviderReference, CreatedOnUtc, UpdatedOnUtc)
            VALUES (@PaymentId, @OrderId, @CustomerId, @Amount, @RefundedAmount, @CurrencyCode, @Method, @Status, @ProviderReference, @CreatedOnUtc, @UpdatedOnUtc);
        """;

    public async Task UpsertAsync(PaymentReadModel payment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payment);

        var connection = await connectionFactory
            .GetOpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        var parameters = new
        {
            payment.PaymentId,
            payment.OrderId,
            payment.CustomerId,
            payment.Amount,
            payment.RefundedAmount,
            payment.CurrencyCode,
            payment.Method,
            Status = payment.Status.ToString(),
            payment.ProviderReference,
            payment.CreatedOnUtc,
            payment.UpdatedOnUtc,
        };

        try
        {
            await connection
                .ExecuteAsync(new CommandDefinition(UpsertPaymentSql, parameters, cancellationToken: cancellationToken))
                .ConfigureAwait(false);
        }
        catch (SqlException sqlException) when (
            sqlException.Number is UniqueIndexViolationErrorNumber or UniqueConstraintViolationErrorNumber)
        {
            throw new ConflictException($"A payment already exists for order {payment.OrderId}.");
        }
    }
}
