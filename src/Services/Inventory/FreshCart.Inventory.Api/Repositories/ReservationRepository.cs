using System.Data;
using Dapper;
using FreshCart.Inventory.Api.Models;

namespace FreshCart.Inventory.Api.Repositories;

public sealed class ReservationRepository(ISqlConnectionFactory connectionFactory) : IReservationRepository
{
    public async Task<StockReservation?> GetByOrderIdAsync(
        Guid orderId,
        IDbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        const string selectByOrderIdSql = """
            SELECT ReservationId, OrderId, Status, CreatedOnUtc, ReleasedOnUtc
            FROM dbo.stock_reservations
            WHERE OrderId = @OrderId;

            SELECT lines.ProductSku, lines.Quantity
            FROM dbo.stock_reservation_lines AS lines
            INNER JOIN dbo.stock_reservations AS reservations
                ON reservations.ReservationId = lines.ReservationId
            WHERE reservations.OrderId = @OrderId
            ORDER BY lines.ProductSku;
            """;

        var connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var resultSets = await connection.QueryMultipleAsync(new CommandDefinition(
                selectByOrderIdSql,
                new { OrderId = orderId },
                transaction,
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        await using (resultSets.ConfigureAwait(false))
        {
            var reservationHeader = await resultSets
                .ReadSingleOrDefaultAsync<ReservationHeaderRow>()
                .ConfigureAwait(false);

            if (reservationHeader is null)
            {
                return null;
            }

            var reservationLines = await resultSets.ReadAsync<StockReservationLine>().ConfigureAwait(false);

            return new StockReservation
            {
                ReservationId = reservationHeader.ReservationId,
                OrderId = reservationHeader.OrderId,
                Status = Enum.Parse<StockReservationStatus>(reservationHeader.Status),
                CreatedOnUtc = reservationHeader.CreatedOnUtc,
                ReleasedOnUtc = reservationHeader.ReleasedOnUtc,
                Lines = reservationLines.ToList(),
            };
        }
    }

    public async Task InsertAsync(StockReservation reservation, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        ArgumentNullException.ThrowIfNull(transaction);

        const string insertReservationSql = """
            INSERT INTO dbo.stock_reservations (ReservationId, OrderId, Status, CreatedOnUtc, ReleasedOnUtc)
            VALUES (@ReservationId, @OrderId, @Status, @CreatedOnUtc, NULL);
            """;

        const string insertLineSql = """
            INSERT INTO dbo.stock_reservation_lines (ReservationId, ProductSku, Quantity)
            VALUES (@ReservationId, @ProductSku, @Quantity);
            """;

        var connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var reservationParameters = new
        {
            reservation.ReservationId,
            reservation.OrderId,
            Status = reservation.Status.ToString(),
            reservation.CreatedOnUtc,
        };

        await connection.ExecuteAsync(new CommandDefinition(
                insertReservationSql,
                reservationParameters,
                transaction,
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        var lineParameters = reservation.Lines
            .Select(line => new { reservation.ReservationId, line.ProductSku, line.Quantity })
            .ToList();

        await connection.ExecuteAsync(new CommandDefinition(
                insertLineSql,
                lineParameters,
                transaction,
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    public async Task<bool> MarkReleasedAsync(
        Guid orderId,
        DateTimeOffset releasedOnUtc,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        const string markReleasedSql = """
            UPDATE dbo.stock_reservations
            SET Status = @ReleasedStatus, ReleasedOnUtc = @ReleasedOnUtc
            WHERE OrderId = @OrderId AND Status = @ActiveStatus;
            """;

        var connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var releaseParameters = new
        {
            OrderId = orderId,
            ReleasedOnUtc = releasedOnUtc,
            ReleasedStatus = nameof(StockReservationStatus.Released),
            ActiveStatus = nameof(StockReservationStatus.Active),
        };

        var affectedRowCount = await connection.ExecuteAsync(new CommandDefinition(
                markReleasedSql,
                releaseParameters,
                transaction,
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        return affectedRowCount > 0;
    }

    private sealed record ReservationHeaderRow(
        Guid ReservationId,
        Guid OrderId,
        string Status,
        DateTimeOffset CreatedOnUtc,
        DateTimeOffset? ReleasedOnUtc);
}
