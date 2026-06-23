using System.Data.Common;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Inventory.Api.Models;
using FreshCart.Inventory.Api.Repositories;
using Microsoft.Data.SqlClient;

namespace FreshCart.Inventory.Api.Services;

/// <summary>
/// Reserves and releases stock inside explicit SQL Server transactions. Inventory is deliberately
/// a plain layered service: quantity bookkeeping is dominated by transactional correctness and raw
/// query latency, so a CQRS or rich-domain stack would add indirection without adding safety.
/// Reserved quantity stays allocated after payment; decrementing on-hand stock at delivery
/// completion is out of scope for this service.
/// </summary>
public sealed partial class StockReservationService(
    ISqlConnectionFactory connectionFactory,
    IStockRepository stockRepository,
    IReservationRepository reservationRepository,
    TimeProvider timeProvider,
    ILogger<StockReservationService> logger) : IStockReservationService
{
    private const string InsufficientStockReason = "Insufficient stock for one or more requested products.";
    private const int SqlServerUniqueConstraintViolationNumber = 2627;
    private const int SqlServerUniqueIndexViolationNumber = 2601;

    public async Task<StockReservationResult> ReserveAsync(
        Guid orderId,
        IReadOnlyCollection<StockReservationLine> requestedLines,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestedLines);
        EnsureReservableLines(orderId, requestedLines);

        var existingReservation = await reservationRepository
            .GetByOrderIdAsync(orderId, transaction: null, cancellationToken)
            .ConfigureAwait(false);

        if (existingReservation is not null)
        {
            return StockReservationResult.Success(existingReservation.ReservationId);
        }

        var reservationLines = MergeAndSortLines(requestedLines);

        var connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false))
        {
            return await ReserveWithinTransactionAsync(orderId, reservationLines, transaction, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public async Task<bool> ReleaseAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false))
        {
            var reservation = await reservationRepository
                .GetByOrderIdAsync(orderId, transaction, cancellationToken)
                .ConfigureAwait(false);

            if (reservation is null || reservation.Status != StockReservationStatus.Active)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return false;
            }

            var releasedOnUtc = timeProvider.GetUtcNow();

            // The conditional UPDATE on Status is the serialization point: a concurrent release
            // matches zero rows here and backs off instead of decrementing the same lines twice.
            var markedReleased = await reservationRepository
                .MarkReleasedAsync(orderId, releasedOnUtc, transaction, cancellationToken)
                .ConfigureAwait(false);

            if (!markedReleased)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return false;
            }

            foreach (var reservationLine in reservation.Lines.OrderBy(line => line.ProductSku, StringComparer.OrdinalIgnoreCase))
            {
                await stockRepository.AdjustQuantityAsync(
                        reservationLine.ProductSku,
                        quantityOnHandDelta: 0,
                        quantityReservedDelta: -reservationLine.Quantity,
                        releasedOnUtc,
                        transaction,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            LogReservationReleased(reservation.ReservationId, orderId);

            return true;
        }
    }

    private async Task<StockReservationResult> ReserveWithinTransactionAsync(
        Guid orderId,
        List<StockReservationLine> reservationLines,
        DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var unavailableSkus = await CollectUnavailableSkusAsync(reservationLines, transaction, cancellationToken)
            .ConfigureAwait(false);

        if (unavailableSkus.Count > 0)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            LogReservationRejected(orderId, unavailableSkus.Count);
            return StockReservationResult.Failure(InsufficientStockReason, unavailableSkus);
        }

        var reservedOnUtc = timeProvider.GetUtcNow();

        foreach (var reservationLine in reservationLines)
        {
            await stockRepository.AdjustQuantityAsync(
                    reservationLine.ProductSku,
                    quantityOnHandDelta: 0,
                    quantityReservedDelta: reservationLine.Quantity,
                    reservedOnUtc,
                    transaction,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var reservation = new StockReservation
        {
            ReservationId = Guid.NewGuid(),
            OrderId = orderId,
            Status = StockReservationStatus.Active,
            CreatedOnUtc = reservedOnUtc,
            Lines = reservationLines,
        };

        try
        {
            await reservationRepository.InsertAsync(reservation, transaction, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SqlException sqlException) when (IsUniqueOrderIdViolation(sqlException))
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return await ResolveConcurrentReservationAsync(orderId, cancellationToken).ConfigureAwait(false);
        }

        LogStockReserved(orderId, reservation.ReservationId, reservationLines.Count);

        return StockReservationResult.Success(reservation.ReservationId);
    }

    private async Task<IReadOnlyList<string>> CollectUnavailableSkusAsync(
        IReadOnlyList<StockReservationLine> reservationLines,
        System.Data.IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var requestedSkus = reservationLines.Select(line => line.ProductSku).ToList();

        var lockedItems = await stockRepository
            .GetBySkusWithUpdateLockAsync(requestedSkus, transaction, cancellationToken)
            .ConfigureAwait(false);

        var lockedItemsBySku = lockedItems.ToDictionary(item => item.ProductSku, StringComparer.OrdinalIgnoreCase);

        return reservationLines
            .Where(line => !lockedItemsBySku.TryGetValue(line.ProductSku, out var stockItem)
                || stockItem.QuantityAvailable < line.Quantity)
            .Select(line => line.ProductSku)
            .ToList();
    }

    private async Task<StockReservationResult> ResolveConcurrentReservationAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var concurrentReservation = await reservationRepository
            .GetByOrderIdAsync(orderId, transaction: null, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new ConflictException(
                $"Reservation state for order \"{orderId}\" changed concurrently. Retry the reservation.");

        LogConcurrentDuplicateReservation(orderId, concurrentReservation.ReservationId);

        return StockReservationResult.Success(concurrentReservation.ReservationId);
    }

    private static List<StockReservationLine> MergeAndSortLines(IReadOnlyCollection<StockReservationLine> requestedLines)
    {
        // Locking stock rows in deterministic sku order prevents writer-writer deadlocks between
        // concurrent reservations that overlap on the same products.
        return requestedLines
            .GroupBy(line => line.ProductSku, StringComparer.OrdinalIgnoreCase)
            .Select(group => new StockReservationLine
            {
                ProductSku = group.Key,
                Quantity = group.Sum(line => line.Quantity),
            })
            .OrderBy(line => line.ProductSku, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void EnsureReservableLines(Guid orderId, IReadOnlyCollection<StockReservationLine> requestedLines)
    {
        if (orderId == Guid.Empty)
        {
            throw new BadRequestException("A stock reservation requires a non-empty order identifier.");
        }

        if (requestedLines.Count == 0)
        {
            throw new BadRequestException("A stock reservation requires at least one line.");
        }

        if (requestedLines.Any(line => string.IsNullOrWhiteSpace(line.ProductSku) || line.Quantity <= 0))
        {
            throw new BadRequestException("Every reservation line requires a product sku and a positive quantity.");
        }
    }

    private static bool IsUniqueOrderIdViolation(SqlException sqlException) =>
        sqlException.Number is SqlServerUniqueConstraintViolationNumber or SqlServerUniqueIndexViolationNumber;

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Stock reservation rejected for order {OrderId}; {UnavailableSkuCount} sku(s) unavailable")]
    private partial void LogReservationRejected(Guid orderId, int unavailableSkuCount);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Reserved stock for order {OrderId} under reservation {ReservationId} covering {LineCount} sku(s)")]
    private partial void LogStockReserved(Guid orderId, Guid reservationId, int lineCount);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Released stock reservation {ReservationId} for order {OrderId}")]
    private partial void LogReservationReleased(Guid reservationId, Guid orderId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Concurrent duplicate reservation detected for order {OrderId}; returning existing reservation {ReservationId}")]
    private partial void LogConcurrentDuplicateReservation(Guid orderId, Guid reservationId);
}
