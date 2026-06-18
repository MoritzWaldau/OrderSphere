namespace OrderSphere.Catalog.Application.Features.Reviews.Admin.GetAllReviews;

public sealed record GetAllReviewsQuery : IQuery<Result<IReadOnlyList<ReviewDto>>>;
