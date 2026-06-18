using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Catalog.Domain.Enums;

namespace OrderSphere.Catalog.Application.Features.Reviews.Public.GetReviewsForProduct;

public sealed class GetReviewsForProductQueryHandler(ICatalogDbContext context)
    : IQueryHandler<GetReviewsForProductQuery, Result<IReadOnlyList<ReviewDto>>>
{
    public async Task<Result<IReadOnlyList<ReviewDto>>> Handle(GetReviewsForProductQuery request, CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);

        var reviews = await context.Reviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId && r.Status == ReviewStatus.Approved)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto(
                r.Id.Value,
                r.ProductId.Value,
                r.Rating,
                r.Title,
                r.Body,
                r.IsVerifiedPurchase,
                r.Status.ToString(),
                r.CreatedAt))
            .ToListAsync(ct);

        return Result<IReadOnlyList<ReviewDto>>.Success(reviews);
    }
}
