using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderSphere.ServiceDefaults;
using OrderSphere.Webhooks.Application.Features.Subscriptions.GetSubscriptions;
using OrderSphere.Webhooks.Application.Features.Subscriptions.GetSubscription;
using OrderSphere.Webhooks.Application.Features.Subscriptions.CreateSubscription;
using OrderSphere.Webhooks.Application.Features.Subscriptions.UpdateSubscription;
using OrderSphere.Webhooks.Application.Features.Subscriptions.DeleteSubscription;
using OrderSphere.Webhooks.Application.Features.Subscriptions.ActivateSubscription;
using OrderSphere.Webhooks.Application.Features.Subscriptions.DeactivateSubscription;
using OrderSphere.Webhooks.Application.Features.Deliveries.GetDeliveries;
using OrderSphere.Webhooks.Domain.Enums;

namespace OrderSphere.Webhooks.Api.Endpoints;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/webhooks")
            .RequireAuthorization();

        group.MapGet("/", GetSubscriptions);
        group.MapGet("/{id:guid}", GetSubscription);
        group.MapPost("/", CreateSubscription);
        group.MapPut("/{id:guid}", UpdateSubscription);
        group.MapDelete("/{id:guid}", DeleteSubscription);
        group.MapPost("/{id:guid}/activate", ActivateSubscription);
        group.MapPost("/{id:guid}/deactivate", DeactivateSubscription);
        group.MapGet("/{id:guid}/deliveries", GetDeliveries);
    }

    private static async Task<IResult> GetSubscriptions(
        IMediator mediator, ClaimsPrincipal user, CancellationToken ct)
    {
        var customerId = GetCustomerId(user);
        if (customerId is null) return Results.Unauthorized();

        var result = await mediator.Send(new GetSubscriptionsQuery(customerId.Value), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetSubscription(
        Guid id, IMediator mediator, ClaimsPrincipal user, CancellationToken ct)
    {
        var customerId = GetCustomerId(user);
        if (customerId is null) return Results.Unauthorized();

        var result = await mediator.Send(new GetSubscriptionQuery(id, customerId.Value), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateSubscription(
        [FromBody] CreateSubscriptionRequest request,
        IMediator mediator, ClaimsPrincipal user, CancellationToken ct)
    {
        var customerId = GetCustomerId(user);
        if (customerId is null) return Results.Unauthorized();

        var result = await mediator.Send(
            new CreateSubscriptionCommand(customerId.Value, request.Url, request.Secret, request.Events), ct);

        return result.ToHttpResult(
            dto => Results.Created($"/api/v1/webhooks/{dto.Id}", dto));
    }

    private static async Task<IResult> UpdateSubscription(
        Guid id, [FromBody] UpdateSubscriptionRequest request,
        IMediator mediator, ClaimsPrincipal user, CancellationToken ct)
    {
        var customerId = GetCustomerId(user);
        if (customerId is null) return Results.Unauthorized();

        var result = await mediator.Send(
            new UpdateSubscriptionCommand(id, customerId.Value, request.Url, request.Secret, request.Events), ct);

        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteSubscription(
        Guid id, IMediator mediator, ClaimsPrincipal user, CancellationToken ct)
    {
        var customerId = GetCustomerId(user);
        if (customerId is null) return Results.Unauthorized();

        var result = await mediator.Send(new DeleteSubscriptionCommand(id, customerId.Value), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ActivateSubscription(
        Guid id, IMediator mediator, ClaimsPrincipal user, CancellationToken ct)
    {
        var customerId = GetCustomerId(user);
        if (customerId is null) return Results.Unauthorized();

        var result = await mediator.Send(new ActivateSubscriptionCommand(id, customerId.Value), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeactivateSubscription(
        Guid id, IMediator mediator, ClaimsPrincipal user, CancellationToken ct)
    {
        var customerId = GetCustomerId(user);
        if (customerId is null) return Results.Unauthorized();

        var result = await mediator.Send(new DeactivateSubscriptionCommand(id, customerId.Value), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetDeliveries(
        Guid id, IMediator mediator, ClaimsPrincipal user, CancellationToken ct,
        int page = 1, int pageSize = 20)
    {
        var customerId = GetCustomerId(user);
        if (customerId is null) return Results.Unauthorized();

        var result = await mediator.Send(
            new GetDeliveriesQuery(id, customerId.Value, page, pageSize), ct);

        return result.ToHttpResult();
    }

    private static Guid? GetCustomerId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

// ── Request models ────────────────────────────────────────────────────────────

public sealed record CreateSubscriptionRequest(
    string Url,
    string? Secret,
    WebhookEventType[] Events);

public sealed record UpdateSubscriptionRequest(
    string Url,
    string? Secret,
    WebhookEventType[] Events);
