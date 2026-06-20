using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace OrderSphere.Catalog.Infrastructure.Blob;

public sealed class AzureBlobStorageService(BlobStorageClients clients, ILogger<AzureBlobStorageService> logger)
    : IBlobStorageService
{
    public bool IsEnabled => clients.IsEnabled;

    public async Task<string> UploadAsync(string blobName, Stream data, string contentType, CancellationToken ct)
    {
        var blobClient = clients.Container!.GetBlobClient(blobName);
        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
        };
        await blobClient.UploadAsync(data, options, ct);
        logger.LogInformation("Uploaded blob {BlobName}.", blobName);
        return blobName;
    }

    public Task<string> GetSasUrlAsync(string blobName, CancellationToken ct = default)
        => clients.GetSasUrlAsync(blobName, ct);
}
