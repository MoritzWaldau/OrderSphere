using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Catalog.Domain.Entities;

namespace OrderSphere.Catalog.Application.Features.Reviews.CreateReview;

public sealed class CreateReviewCommandHandler(ICatalogDbContext context, IOrderingClient ordering)
    : ICommandHandler<CreateReviewCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateReviewCommand request, CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);
        var customerId = CustomerId.From(request.CustomerId);

        var product = await context.Products
            .AsTracking()
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive, ct);

        if (product is null)
            return Result<Guid>.Failure(ProductErrors.NotFound);

        var alreadyReviewed = await context.Reviews
            .AsNoTracking()
            .AnyAsync(r => r.ProductId == productId && r.CustomerId == customerId, ct);

        if (alreadyReviewed)
            return Result<Guid>.Failure(ReviewErrors.AlreadyReviewed);

        var purchased = await ordering.HasPurchasedAsync(request.CustomerId, request.ProductId, ct);
        if (!purchased)
            return Result<Guid>.Failure(ReviewErrors.NotPurchased);

        var reviewResult = ProductReview.Create(
            productId, customerId, request.Rating, request.Title, request.Body, isVerifiedPurchase: true);

        if (reviewResult.IsFailure)
            return Result<Guid>.Failure(reviewResult.Error);

        var review = reviewResult.Value;
        context.Reviews.Add(review);
        await context.SaveChangesAsync(ct);

        // Fold the new review into the product's cached summary.
        await RatingSummaryUpdater.RecomputeAsync(context, product, ct);
        await context.SaveChangesAsync(ct);

        return Result<Guid>.Success(review.Id.Value);
    }
}
