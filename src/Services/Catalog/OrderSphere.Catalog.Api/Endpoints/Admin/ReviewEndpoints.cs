using MediatR;
using OrderSphere.Catalog.Application.Features.Reviews.Admin.GetAllReviews;
using OrderSphere.Catalog.Application.Features.Reviews.Admin.ModerateReview;
using OrderSphere.ServiceDefaults;

namespace OrderSphere.Catalog.Api.Endpoints.Admin;

public static class ReviewEndpoints
{
    public static void MapAdminReviewEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAll)
            .WithName("GetAllReviewsAdmin")
            .WithTags("AdminReviews");

        group.MapPost("/{reviewId:guid}/moderate", Moderate)
            .WithName("ModerateReview")
            .WithTags("AdminReviews");
    }

    private static async Task<IResult> GetAll(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllReviewsQuery(), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Moderate(
        Guid reviewId, ModerateReviewRequest body, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ModerateReviewCommand(reviewId, body.Approve), ct);
        return result.ToHttpResult(() => Results.Ok());
    }

    public sealed record ModerateReviewRequest(bool Approve);
}
