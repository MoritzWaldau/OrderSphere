using OrderSphere.Catalog.Application.Abstractions;

namespace OrderSphere.Catalog.Api.BackgroundServices;

/// <summary>
/// On startup, ensures the Azure AI Search index exists and seeds it from the catalog
/// database when it is empty (e.g. first deployment). No-op when search is not
/// configured. Reindexing on demand is exposed via the admin reindex endpoint.
/// </summary>
public sealed class CatalogSearchInitializer(
    IServiceScopeFactory scopeFactory,
    ILogger<CatalogSearchInitializer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var searchIndex = scope.ServiceProvider.GetRequiredService<IProductSearchIndex>();

        if (!searchIndex.IsEnabled)
            return;

        try
        {
            await searchIndex.EnsureSeededAsync(stoppingToken);
            logger.LogInformation("Catalog search index is ready.");
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
        catch (Exception ex)
        {
            // Search is an optional read model — a failure here must not stop the service.
            logger.LogError(ex, "Catalog search index initialization failed; falling back to database search.");
        }
    }
}
