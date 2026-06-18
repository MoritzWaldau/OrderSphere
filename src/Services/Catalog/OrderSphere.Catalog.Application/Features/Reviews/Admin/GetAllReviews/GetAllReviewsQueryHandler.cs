namespace OrderSphere.Catalog.Application.Features.Reviews.Admin.GetAllReviews;

public sealed class GetAllReviewsQueryHandler(ICatalogDbContext context)
    : IQueryHandler<GetAllReviewsQuery, Result<IReadOnlyList<ReviewDto>>>
{
    public async Task<Result<IReadOnlyList<ReviewDto>>> Handle(GetAllReviewsQuery request, CancellationToken ct)
    {
        var reviews = await context.Reviews
            .AsNoTracking()
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
