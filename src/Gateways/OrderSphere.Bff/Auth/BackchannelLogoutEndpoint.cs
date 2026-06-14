using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using OrderSphere.BuildingBlocks.Security;

namespace OrderSphere.Bff.Auth;

/// <summary>
/// Handles Auth0 back-channel logout requests (OIDC Front-Channel Logout draft / OIDC CIBA).
///
/// Flow:
///   1. Auth0 POSTs application/x-www-form-urlencoded { logout_token: "&lt;JWT&gt;" }.
///   2. This endpoint validates the JWT (iss, aud, sid, events, no nonce, valid signature).
///   3. Resolves the Auth0 session_id → Redis session key via the secondary index written
///      by RedisTicketStore.StoreAsync.
///   4. Calls ITicketStore.RemoveAsync to revoke the session from Redis.
///   5. Emits a SecurityAuditEvent for the audit log.
///
/// Auth0 application configuration required (BFF application):
///   Back-Channel Logout URI = https://{bff-host}/bff/backchannel-logout
///
/// Reference: https://openid.net/specs/openid-connect-backchannel-1_0.html
/// </summary>
public static class BackchannelLogoutEndpoint
{
    private const string BackchannelLogoutEvent =
        "http://schemas.openid.net/event/backchannel-logout";

    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/bff/backchannel-logout", HandleAsync)
            .AllowAnonymous()
            .WithName("BffBackchannelLogout");

        return app;
    }

    private static async Task<IResult> HandleAsync(
        HttpRequest request,
        IConfiguration config,
        IConfigurationManager<OpenIdConnectConfiguration> oidcConfigManager,
        ITicketStore ticketStore,
        ISecurityAuditLogger auditLogger,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        // ── 1. Parse form body ───────────────────────────────────────────────
        if (!request.HasFormContentType)
        {
            logger.LogWarning("Back-channel logout request missing form content type.");
            return Results.BadRequest("Expected application/x-www-form-urlencoded.");
        }

        var form = await request.ReadFormAsync(ct);
        var logoutToken = form["logout_token"].FirstOrDefault();

        if (string.IsNullOrEmpty(logoutToken))
        {
            logger.LogWarning("Back-channel logout request missing logout_token field.");
            return Results.BadRequest("Missing logout_token.");
        }

        // ── 2. Fetch JWKS from Auth0 (cached by IConfigurationManager) ───
        var authority = config["Oidc:Authority"]!;
        var clientId = config["Oidc:ClientId"] ?? "web-bff";

        OpenIdConnectConfiguration oidcConfig;
        try
        {
            oidcConfig = await oidcConfigManager.GetConfigurationAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch OIDC configuration for logout_token validation.");
            return Results.Problem(statusCode: StatusCodes.Status500InternalServerError,
                title: "OIDC configuration unavailable.");
        }

        // ── 3. Validate JWT signature + standard claims ──────────────────────
        var handler = new JsonWebTokenHandler { MapInboundClaims = false };
        var validationParams = new TokenValidationParameters
        {
            ValidIssuer = authority,
            ValidAudience = clientId,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false, // logout tokens carry iat but not exp
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = oidcConfig.SigningKeys,
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        TokenValidationResult validationResult;
        try
        {
            validationResult = await handler.ValidateTokenAsync(logoutToken, validationParams);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "logout_token signature validation threw an exception.");
            auditLogger.Log(new SecurityAuditEvent(
                SecurityAuditEventType.TokenValidationFailed,
                Details: "Back-channel logout token validation exception: " + ex.Message));
            return Results.BadRequest("Invalid logout_token.");
        }

        if (!validationResult.IsValid)
        {
            logger.LogWarning("logout_token failed validation: {Reason}",
                validationResult.Exception?.Message ?? "unknown");
            auditLogger.Log(new SecurityAuditEvent(
                SecurityAuditEventType.TokenValidationFailed,
                Details: "logout_token invalid: " + validationResult.Exception?.Message));
            return Results.BadRequest("Invalid logout_token.");
        }

        var claims = validationResult.Claims;

        // ── 4. Check OIDC back-channel logout spec requirements ───────────────
        // MUST contain the events claim with the backchannel-logout event.
        if (!claims.TryGetValue("events", out var eventsObj)
            || eventsObj?.ToString()?.Contains(BackchannelLogoutEvent) != true)
        {
            logger.LogWarning("logout_token missing or invalid 'events' claim.");
            return Results.BadRequest("logout_token is not a back-channel logout token.");
        }

        // MUST NOT contain a nonce claim.
        if (claims.ContainsKey("nonce"))
        {
            logger.LogWarning("logout_token contains forbidden 'nonce' claim.");
            return Results.BadRequest("logout_token must not contain a nonce.");
        }

        // MUST contain sid (or sub — sid preferred for targeted revocation).
        if (!claims.TryGetValue("sid", out var sidObj) || sidObj is not string sid
            || string.IsNullOrEmpty(sid))
        {
            logger.LogWarning("logout_token missing 'sid' claim. Cannot revoke specific session.");
            // Return 200 so Auth0 does not retry; revocation is not possible without sid.
            return Results.Ok();
        }

        var sub = claims.TryGetValue("sub", out var subObj) ? subObj?.ToString() : null;

        auditLogger.Log(new SecurityAuditEvent(
            SecurityAuditEventType.BackchannelLogoutReceived,
            UserId: sub,
            SessionId: sid,
            IpAddress: request.HttpContext.Connection.RemoteIpAddress?.ToString()));

        // ── 5. Look up session key via sid index ─────────────────────────────
        if (ticketStore is not RedisTicketStore redisStore)
        {
            logger.LogError("ITicketStore is not RedisTicketStore; cannot look up session by sid.");
            return Results.Ok(); // Do not expose internal errors to Auth0
        }

        var sessionKey = await redisStore.FindKeyBySessionIdAsync(sid);
        if (sessionKey is null)
        {
            logger.LogInformation(
                "Back-channel logout: no active session found for sid={Sid} (already expired or logged out).", sid);
            return Results.Ok();
        }

        // ── 6. Revoke the session ─────────────────────────────────────────────
        await ticketStore.RemoveAsync(sessionKey);

        auditLogger.Log(new SecurityAuditEvent(
            SecurityAuditEventType.BackchannelLogoutRevoked,
            UserId: sub,
            SessionId: sid,
            Details: $"Session key {sessionKey} removed from Redis."));

        logger.LogInformation(
            "Back-channel logout: session revoked for sid={Sid}, key={Key}.", sid, sessionKey);

        return Results.Ok();
    }
}
