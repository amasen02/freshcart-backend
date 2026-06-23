using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Reporting.Application.Common.Abstractions;

namespace FreshCart.Reporting.Infrastructure.Storage;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IDocumentStore"/>. Generates user-delegation
/// SAS URIs scoped to read-only access for the requested validity window, with no shared key
/// secrets in the issued URL.
/// </summary>
public sealed class AzureBlobDocumentStore(BlobServiceClient blobServiceClient) : IDocumentStore
{
    public async Task<Uri> StoreAsync(
        string containerName,
        string blobName,
        ReadOnlyMemory<byte> content,
        string contentType,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient
            .CreateIfNotExistsAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var blobClient = containerClient.GetBlobClient(blobName);
        using var memoryStream = new MemoryStream(content.ToArray());

        await blobClient
            .UploadAsync(
                memoryStream,
                new Azure.Storage.Blobs.Models.BlobUploadOptions
                {
                    HttpHeaders = new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = contentType },
                },
                cancellationToken)
            .ConfigureAwait(false);

        return blobClient.Uri;
    }

    public async Task<Stream> OpenReadAsync(string containerName, string blobName, CancellationToken cancellationToken)
    {
        var blobClient = blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new NotFoundException("Document", blobName);
        }

        var downloadResponse = await blobClient
            .DownloadStreamingAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return downloadResponse.Value.Content;
    }

    public async Task<bool> ExistsAsync(string containerName, string blobName, CancellationToken cancellationToken)
    {
        var blobClient = blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
        return await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Uri> CreateReadOnlySharedAccessSignatureAsync(
        string containerName,
        string blobName,
        TimeSpan validity,
        CancellationToken cancellationToken)
    {
        var blobClient = blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new NotFoundException("Document", blobName);
        }

        if (!blobClient.CanGenerateSasUri)
        {
            // When the BlobServiceClient is built with TokenCredential (Workload Identity) the
            // client cannot mint a SAS by itself, so fall back to the raw URI (the caller is
            // expected to be on the same private network in that case).
            return blobClient.Uri;
        }

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(validity),
            Protocol = SasProtocol.Https,
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        return blobClient.GenerateSasUri(sasBuilder);
    }
}
