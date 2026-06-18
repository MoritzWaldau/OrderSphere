using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Catalog.Domain.Errors;

public static class ReviewErrors
{
    public static readonly Error NotFound = new("Review.NotFound", "Review was not found.", ErrorType.NotFound);
    public static readonly Error AlreadyReviewed = new("Review.AlreadyReviewed", "You have already reviewed this product.", ErrorType.Conflict);
    public static readonly Error NotPurchased = new("Review.NotPurchased", "Only customers who purchased this product can review it.", ErrorType.Validation);
    public static readonly Error InvalidRating = new("Review.InvalidRating", "Rating must be between 1 and 5.", ErrorType.Validation);
}
