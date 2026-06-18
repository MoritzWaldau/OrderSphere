namespace OrderSphere.Catalog.Application.Features.Reviews.Public.GetReviewsForProduct;

public sealed record GetReviewsForProductQuery(Guid ProductId)
    : IQuery<Result<IReadOnlyList<ReviewDto>>>;
