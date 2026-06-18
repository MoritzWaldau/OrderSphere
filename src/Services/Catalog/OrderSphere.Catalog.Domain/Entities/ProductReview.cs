using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Catalog.Domain.Enums;
using OrderSphere.Catalog.Domain.Errors;

namespace OrderSphere.Catalog.Domain.Entities;

public sealed class ProductReview : AuditableEntity<ReviewId>, IAggregateRoot
{
    public ProductId ProductId { get; private set; }
    public CustomerId CustomerId { get; private set; }
    public int Rating { get; private set; }
    public string Title { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public bool IsVerifiedPurchase { get; private set; }
    public ReviewStatus Status { get; private set; } = ReviewStatus.Approved;

    // Parameterless constructor for EF Core materialisation.
    private ProductReview() { }

    private ProductReview(ProductId productId, CustomerId customerId, int rating, string title, string body, bool isVerifiedPurchase)
    {
        Id = ReviewId.New();
        ProductId = productId;
        CustomerId = customerId;
        Rating = rating;
        Title = title;
        Body = body;
        IsVerifiedPurchase = isVerifiedPurchase;
        Status = ReviewStatus.Approved;
    }

    public static Result<ProductReview> Create(
        ProductId productId, CustomerId customerId, int rating, string title, string body, bool isVerifiedPurchase)
    {
        if (rating is < 1 or > 5)
            return Result<ProductReview>.Failure(ReviewErrors.InvalidRating);

        return Result<ProductReview>.Success(
            new ProductReview(productId, customerId, rating, title.Trim(), body.Trim(), isVerifiedPurchase));
    }

    public void Approve() => Status = ReviewStatus.Approved;

    public void Reject() => Status = ReviewStatus.Rejected;
}
