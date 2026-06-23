namespace FreshCart.Reporting.Application.Common.Abstractions;

/// <summary>
/// Append-only blob store for rendered invoices and exported reports. Implementations target
/// Azure Blob Storage in production and the local filesystem in tests.
/// </summary>
public interface IDocumentStore
{
    Task<Uri> StoreAsync(
        string containerName,
        string blobName,
        ReadOnlyMemory<byte> content,
        string contentType,
        CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken);

    Task<Uri> CreateReadOnlySharedAccessSignatureAsync(
        string containerName,
        string blobName,
        TimeSpan validity,
        CancellationToken cancellationToken);
}
