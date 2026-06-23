using JasperFx;
using Marten;

namespace FreshCart.Basket.Api.Persistence;

/// <summary>
/// Shared optimistic-concurrency retry for Marten document writes. Loads the document, applies a pure
/// in-memory mutation and saves under the version check; on a losing race it drops the queued
/// operation, reloads the winning snapshot and re-applies, so concurrent edits merge instead of one
/// silently overwriting the other.
/// </summary>
public static class MartenConcurrencyRetry
{
    public const int DefaultMaxAttempts = 3;

    /// <summary>
    /// Runs the load-mutate-save cycle for one document identified by <paramref name="documentId"/>.
    /// Returns true when a write was persisted, false when <paramref name="mutate"/> returned null and
    /// nothing needed saving.
    /// </summary>
    public static async Task<bool> ExecuteAsync<TDocument>(
        IDocumentSession documentSession,
        Guid documentId,
        Func<TDocument?, TDocument?> mutate,
        CancellationToken cancellationToken,
        int maxAttempts = DefaultMaxAttempts)
        where TDocument : class
    {
        ArgumentNullException.ThrowIfNull(documentSession);
        ArgumentNullException.ThrowIfNull(mutate);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

        ConcurrencyException lastConflict;
        var attempt = 0;
        do
        {
            attempt++;
            var current = await documentSession
                .LoadAsync<TDocument>(documentId, cancellationToken)
                .ConfigureAwait(false);

            var mutated = mutate(current);
            if (mutated is null)
            {
                return false;
            }

            documentSession.StoreObjects([mutated]);

            try
            {
                await documentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (ConcurrencyException conflict)
            {
                // The version moved under us. Drop the queued write and reload the winning snapshot on
                // the next iteration; once the attempts are exhausted the conflict propagates.
                documentSession.EjectAllPendingChanges();
                lastConflict = conflict;
            }
        }
        while (attempt < maxAttempts);

        throw lastConflict;
    }
}
