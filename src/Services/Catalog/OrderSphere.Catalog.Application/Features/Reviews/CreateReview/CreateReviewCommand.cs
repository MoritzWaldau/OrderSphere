namespace OrderSphere.Catalog.Application.Features.Reviews.CreateReview;

public sealed record CreateReviewCommand(
    Guid ProductId,
    Guid CustomerId,
    int Rating,
    string Title,
    string Body) : ICommand<Result<Guid>>;
