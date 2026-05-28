using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Webhooks.Domain.Entities;
using OrderSphere.Webhooks.Domain.Enums;
using OrderSphere.Webhooks.Infrastructure.Persistence;

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
        WebhooksDbContext db,
        ClaimsPrincipal user)
    {
        var customerId = GetCustomerId(user);
        if (customerId is null) return Results.Unauthorized();

        var subs = await db.Subscriptions
            .Where(s => s.CustomerId == CustomerId.From(customerId.Value) && !s.IsDeleted)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SubscriptionDto(
                s.Id.Value, s.Url, s.Events, s.IsActive, s.CreatedAt, s.UpdatedAt))
            .ToListAsync();

        return Results.Ok(subs);
    }

    private static async Task<IResult> GetSubscription(
        Guid id,
        WebhooksDbContext db,
        ClaimsPrincipal user)
    {
        var customerId = GetCustomerId(user);
        if (customerId is null) return Results.Unauthorized();

        var sub = await db.Subscriptions
            .Where(s => s.Id == WebhookSubscriptionId.From(id) && s.CustomerId == CustomerId.From(customerId.Value) && !s.IsDeleted)
            .Select(s => new SubscriptionDto(
                s.Id.Value, s.Url, s.Events, s.IsActive, s.CreatedAt, s.UpdatedAt))
            .FirstOrDefaultAsync();

        return sub is null ? Results.NotFound() : Results.Ok(sub);
    }

    private static async Task<IResult> CreateSubscription(
        CreateSubscriptionRequest request,
        WebhooksDbContext db,
        ClaimsPrincipal user)
    {
        var customerId = GetCustomerId(user);
        if (customerId is null) return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Url) || !Uri.TryCreate(request.Url, UriKind.Absolute, out var uri))
            return Results.BadRequest("A valid absolute URL is required.");

        if (uri.Scheme != "https")
            return Results.BadRequest("Only HTTPS URLs are accepted.");

        if (request.Events is null || request.Events.Length == 0)
            return Results.BadRequest("At least one event type is required.");

        var secret = request.Secret;
        if (string.IsNullOrWhiteSpace(secret))
            secret = Convert.ToBase64String(Guid.NewGuid().ToByteArray())[..32];

        var subscription = new WebhookSubscription(
            CustomerId.From(customerId.Value),
            request.Url,
            secret,
            request.Events);

        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/v1/webhooks/{subscription.Id.Value}",
            new SubscriptionCreatedDto(subscription.Id.Value, secret));
    }

    private static async Task<IResult> UpdateSubscription(
        Guid id,
        UpdateSubscriptionRequest request,
        WebhooksDbContext db,
        ClaimsPrincipal user)
    {
        var customerId = GetCustomerId(user);
        if (customerId is null) return Results.Unauthorized();

        var sub = await db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == WebhookSubscriptionId.From(id) && s.CustomerId == CustomerId.From(customerId.Value) && !s.IsDeleted);

        if (sub is null) return Results.NotFound();

        if (string.IsNullOrWhiteSpace(request.Url) || !Uri.TryCreate(request.Url, UriKind.Absolute, out var uri))
            return Results.BadRequest("A valid absolute URL is required.");

        if (uri.Scheme != "https")
            return Results.BadRequest("Only HTTPS URLs are accepted.");

        if (request.Events is null || request.Events.Length == 0)
            return Results.BadRequest("At least one event type is required.");

        sub.Update(request.Url, request.Secret ?? sub.Secret, request.Events);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> DeleteSubscription(
        Guid id,
        WebhooksDbContext db,
        ClaimsPrincipal user)
    {
        var customerId = GetCustomerId(user);
        if (customerId is null) return Results.Unauthorized();

        var sub = await db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == WebhookSubscriptionId.From(id) && s.CustomerId == CustomerId.From(customerId.Value) && !s.IsDeleted);

        if (sub is null) return Results.NotFound();

        sub.IsDeleted = true;
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> ActivateSubscription(
        Guid id,
        WebhooksDbContext db,
        ClaimsPrincipal user)
    {
        var customerId = GetCustomerId(user);
        if (customerId is null) return Results.Unauthorized();

        var sub = await db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == WebhookSubscriptionId.From(id) && s.CustomerId == CustomerId.From(customerId.Value) && !s.IsDeleted);

        if (sub is null) return Results.NotFound();

        sub.Activate();
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> DeactivateSubscription(
        Guid id,
        WebhooksDbContext db,
        ClaimsPrincipal user)
    {
        var customerId = GetCustomerId(user);
        if (customerId is null) return Results.Unauthorized();

        var sub = await db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == WebhookSubscriptionId.From(id) && s.CustomerId == CustomerId.From(customerId.Value) && !s.IsDeleted);

        if (sub is null) return Results.NotFound();

        sub.Deactivate();
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> GetDeliveries(
        Guid id,
        WebhooksDbContext db,
        ClaimsPrincipal user,
        int page = 1,
        int pageSize = 20)
    {
        var customerId = GetCustomerId(user);
        if (customerId is null) return Results.Unauthorized();

        // Verify the subscription belongs to the caller.
        var owns = await db.Subscriptions
            .AnyAsync(s => s.Id == WebhookSubscriptionId.From(id) && s.CustomerId == CustomerId.From(customerId.Value) && !s.IsDeleted);

        if (!owns) return Results.NotFound();

        var deliveries = await db.Deliveries
            .Where(d => d.SubscriptionId == WebhookSubscriptionId.From(id))
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DeliveryDto(
                d.Id.Value, d.EventType, d.EventId, d.Status.ToString(),
                d.AttemptCount, d.LastHttpStatus, d.LastError,
                d.CreatedAt, d.UpdatedAt))
            .ToListAsync();

        return Results.Ok(deliveries);
    }

    private static Guid? GetCustomerId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record SubscriptionDto(
    Guid Id, string Url, string Events, bool IsActive,
    DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record SubscriptionCreatedDto(Guid Id, string Secret);

public sealed record CreateSubscriptionRequest(
    string Url,
    string? Secret,
    WebhookEventType[] Events);

public sealed record UpdateSubscriptionRequest(
    string Url,
    string? Secret,
    WebhookEventType[] Events);

public sealed record DeliveryDto(
    Guid Id, string EventType, Guid EventId, string Status,
    int AttemptCount, int? LastHttpStatus, string? LastError,
    DateTime CreatedAt, DateTime? UpdatedAt);
