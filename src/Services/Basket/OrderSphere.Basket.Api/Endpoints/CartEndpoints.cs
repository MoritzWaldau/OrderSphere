using Asp.Versioning;
using MediatR;
using OrderSphere.Basket.Api.Configuration;
using OrderSphere.Basket.Application.Features.Cart.AddToCart;
using OrderSphere.Basket.Application.Features.Cart.DecreaseCartItem;
using OrderSphere.Basket.Application.Features.Cart.GetCart;
using OrderSphere.Basket.Application.Features.Cart.RemoveFromCart;
using OrderSphere.BuildingBlocks.Security;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.ServiceDefaults;

namespace OrderSphere.Basket.Api.Endpoints;

public static class CartEndpoints
{
    public static void MapCartEndpoints(this WebApplication app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app
            .MapGroup("api/v{version:apiVersion}/cart")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(1.0)
            .MapToApiVersion(1.0)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitingExtensions.CartPolicy);

        group.MapGet("/", async (ICurrentUser currentUser, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetCustomerId(currentUser, out var customerId))
                return Results.Unauthorized();

            var result = await mediator.Send(new GetCartQuery(customerId), ct);
            return result.ToHttpResult();
        });

        group.MapPost("/add", async (AddToCartRequest req, ICurrentUser currentUser, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetCustomerId(currentUser, out var customerId))
                return Results.Unauthorized();

            var result = await mediator.Send(
                new AddToCartCommand(customerId, ProductId.From(req.ProductId), req.Quantity), ct);
            return result.ToHttpResult();
        });

        group.MapDelete("/remove", async (Guid productId, ICurrentUser currentUser, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetCustomerId(currentUser, out var customerId))
                return Results.Unauthorized();

            var result = await mediator.Send(
                new RemoveFromCartCommand(customerId, ProductId.From(productId)), ct);
            return result.ToHttpResult();
        });

        group.MapPut("/decrease", async (DecreaseCartItemRequest req, ICurrentUser currentUser, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetCustomerId(currentUser, out var customerId))
                return Results.Unauthorized();

            var result = await mediator.Send(
                new DecreaseCartItemQuantityCommand(customerId, ProductId.From(req.ProductId)), ct);
            return result.ToHttpResult();
        });
    }

    private static bool TryGetCustomerId(ICurrentUser currentUser, out CustomerId customerId)
    {
        customerId = CustomerId.Empty;
        if (!currentUser.IsAuthenticated || currentUser.Sub is null)
            return false;

        customerId = CustomerId.FromSub(currentUser.Sub);
        return true;
    }
}

public sealed record AddToCartRequest(Guid ProductId, int Quantity);
public sealed record DecreaseCartItemRequest(Guid ProductId);
