using OrderSphere.Catalog.Domain.Entities;
using OrderSphere.Catalog.Domain.Enums;

namespace OrderSphere.Catalog.Application.Features.Reviews;

/// <summary>
/// Recomputes a product's cached rating summary from its approved reviews.
/// The product must be tracked by the context; the caller persists the change.
/// </summary>
internal static class RatingSummaryUpdater
{
    public static async Task RecomputeAsync(ICatalogDbContext context, Product product, CancellationToken ct)
    {
        var ratings = await context.Reviews
            .AsNoTracking()
            .Where(r => r.ProductId == product.Id && r.Status == ReviewStatus.Approved)
            .Select(r => r.Rating)
            .ToListAsync(ct);

        product.SetRatingSummary(ratings.Count == 0 ? 0d : ratings.Average(), ratings.Count);
    }
}
