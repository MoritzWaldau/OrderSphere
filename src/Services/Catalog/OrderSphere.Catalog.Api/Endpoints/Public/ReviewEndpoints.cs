using MediatR;
using OrderSphere.BuildingBlocks.Security;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Catalog.Application.Features.Reviews.CreateReview;
using OrderSphere.Catalog.Application.Features.Reviews.Public.GetReviewsForProduct;
using OrderSphere.ServiceDefaults;

namespace OrderSphere.Catalog.Api.Endpoints.Public;

public static class ReviewEndpoints
{
    public static void MapPublicReviewEndpoints(this RouteGroupBuilder group)
    {
        // Public: approved reviews for a product.
        group.MapGet("/product/{productId:guid}", GetForProduct)
            .WithName("GetReviewsForProduct")
            .WithTags("Reviews");

        // Authenticated: a customer who purchased the product posts a review.
        group.MapPost("/product/{productId:guid}", CreateReview)
            .RequireAuthorization()
            .WithName("CreateReview")
            .WithTags("Reviews");
    }

    private static async Task<IResult> GetForProduct(
        Guid productId, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetReviewsForProductQuery(productId), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateReview(
        Guid productId,
        CreateReviewRequest body,
        ICurrentUser currentUser,
        IMediator mediator,
        CancellationToken ct)
    {
        if (currentUser.Sub is null)
            return Results.Unauthorized();

        var customerId = CustomerId.FromSub(currentUser.Sub).Value;

        var result = await mediator.Send(
            new CreateReviewCommand(productId, customerId, body.Rating, body.Title, body.Body), ct);

        return result.ToHttpResult(id => Results.Created($"/api/v1/reviews/{id}", new { Id = id }));
    }

    public sealed record CreateReviewRequest(int Rating, string Title, string Body);
}
