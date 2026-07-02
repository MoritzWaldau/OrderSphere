using Microsoft.EntityFrameworkCore;
using OrderSphere.UserProfile.Application.Abstractions;
using OrderSphere.UserProfile.Application.Models;

namespace OrderSphere.UserProfile.Api.Endpoints;

/// <summary>
/// Service-internal endpoints not exposed through the API gateway.
/// Called by the Notification.Worker to fetch per-user channel preferences.
/// </summary>
public static class InternalProfileEndpoints
{
    public static void MapInternalProfileEndpoints(this WebApplication app)
    {
        // D4 — requires a valid client-credentials token (any authenticated caller); M2M
        // tokens carry no role claims, so no role-based policy is applied here.
        var group = app.MapGroup("internal/profiles").RequireAuthorization();

        // Notification.Worker calls this to decide which channels to activate.
        // Lookup is by customer email; returns defaults (email-only) when profile not found.
        group.MapGet("by-email/{email}/notification-preferences",
            async (string email, IUserProfileDbContext db, CancellationToken ct) =>
            {
                var profile = await db.CustomerProfiles
                    .FirstOrDefaultAsync(p => p.Email == email, ct);

                if (profile is null)
                    return Results.Ok(new NotificationPreferencesDto(true, false, false, null));

                var prefs = profile.NotificationPreferences;
                return Results.Ok(new NotificationPreferencesDto(
                    prefs.EmailEnabled, prefs.SmsEnabled, prefs.PushEnabled, prefs.ConsentedAt));
            });
    }
}
