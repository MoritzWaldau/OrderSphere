using OrderSphere.BuildingBlocks.Infrastructure.Blob;

namespace OrderSphere.Catalog.Api.BackgroundServices;

/// <summary>
/// On startup, ensures the product-images blob container exists (idempotent).
/// No-op when blob storage is not configured.
/// </summary>
public sealed class BlobContainerInitializer(
    BlobStorageClients clients,
    ILogger<BlobContainerInitializer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!clients.IsEnabled)
            return;

        try
        {
            await clients.Container!.CreateIfNotExistsAsync(
                Azure.Storage.Blobs.Models.PublicAccessType.None,
                cancellationToken: stoppingToken);

            logger.LogInformation("Blob container '{ContainerName}' is ready.", clients.ContainerName);
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Blob container initialization failed; image uploads will be unavailable.");
        }
    }
}
