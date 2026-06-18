namespace OrderSphere.Catalog.Application.Features.Reviews.Admin.ModerateReview;

/// <summary>Approves or rejects (hides) a review. Approved reviews are public and counted
/// in the product rating summary; rejected reviews are excluded from both.</summary>
public sealed record ModerateReviewCommand(Guid ReviewId, bool Approve) : ICommand<Result>;
