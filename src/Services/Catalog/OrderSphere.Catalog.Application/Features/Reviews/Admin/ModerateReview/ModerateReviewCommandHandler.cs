using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Catalog.Application.Features.Reviews.Admin.ModerateReview;

public sealed class ModerateReviewCommandHandler(ICatalogDbContext context)
    : ICommandHandler<ModerateReviewCommand, Result>
{
    public async Task<Result> Handle(ModerateReviewCommand request, CancellationToken ct)
    {
        var review = await context.Reviews
            .AsTracking()
            .FirstOrDefaultAsync(r => r.Id == ReviewId.From(request.ReviewId), ct);

        if (review is null)
            return Result.Failure(ReviewErrors.NotFound);

        if (request.Approve)
            review.Approve();
        else
            review.Reject();

        var product = await context.Products
            .AsTracking()
            .FirstOrDefaultAsync(p => p.Id == review.ProductId, ct);

        await context.SaveChangesAsync(ct);

        if (product is not null)
        {
            await RatingSummaryUpdater.RecomputeAsync(context, product, ct);
            await context.SaveChangesAsync(ct);
        }

        return Result.Success();
    }
}
