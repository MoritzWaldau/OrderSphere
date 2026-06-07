using Microsoft.EntityFrameworkCore;
using OrderSphere.Advisory.Application.Abstractions;

namespace OrderSphere.Advisory.Api.Agent;

public sealed record ConversationSummaryDto(
    string ConversationId, string? LastMessage, DateTime UpdatedAt, int MessageCount);

public sealed record ConversationMessageDto(string Role, string Text, DateTime CreatedAt);

// Read-only history of the current customer's conversations. Scoped by the caller's
// Keycloak subject; a caller without a resolvable subject sees nothing.
public static class HistoryEndpoints
{
    public static IEndpointRouteBuilder MapAdvisorHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/conversations", async (
            IAdvisoryDbContext db, HttpContext http, CancellationToken ct) =>
        {
            var sub = CustomerContext.ResolveSub(http.User);
            if (sub is null)
            {
                return Results.Unauthorized();
            }

            var conversations = await db.Conversations
                .Where(c => c.CustomerSub == sub)
                .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
                .Select(c => new ConversationSummaryDto(
                    c.ConversationKey,
                    c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Text).FirstOrDefault(),
                    c.UpdatedAt ?? c.CreatedAt,
                    c.Messages.Count))
                .ToListAsync(ct);

            return Results.Ok(conversations);
        });

        app.MapGet("/conversations/{conversationId}", async (
            string conversationId, IAdvisoryDbContext db, HttpContext http, CancellationToken ct) =>
        {
            var sub = CustomerContext.ResolveSub(http.User);
            if (sub is null)
            {
                return Results.Unauthorized();
            }

            var messages = await db.Conversations
                .Where(c => c.CustomerSub == sub && c.ConversationKey == conversationId)
                .SelectMany(c => c.Messages)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ConversationMessageDto(m.Role, m.Text, m.CreatedAt))
                .ToListAsync(ct);

            return messages.Count == 0 ? Results.NotFound() : Results.Ok(messages);
        });

        return app;
    }
}
