using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Security;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// ISecurityAuditLogger implementation that writes structured log entries via
/// the standard ILogger pipeline. OpenTelemetry (wired in ServiceDefaults) carries
/// these entries to the configured telemetry sink with the originating trace_id attached.
/// </summary>
internal sealed class SecurityAuditLogger(ILogger<SecurityAuditLogger> logger)
    : ISecurityAuditLogger
{
    public void Log(SecurityAuditEvent evt)
    {
        var level = evt.Type switch
        {
            SecurityAuditEventType.LoginFailure => LogLevel.Warning,
            SecurityAuditEventType.RefreshTokenRevoked => LogLevel.Warning,
            SecurityAuditEventType.TokenValidationFailed => LogLevel.Warning,
            SecurityAuditEventType.AntiforgeryValidationFailed => LogLevel.Warning,
            SecurityAuditEventType.AuthorizationDenied => LogLevel.Information,
            _ => LogLevel.Information,
        };

        logger.Log(
            level,
            "SECURITY_AUDIT | {EventType} | user={UserId} | sid={SessionId} | ip={IpAddress} | {Details} | ts={OccurredAt:o}",
            evt.Type,
            evt.UserId    ?? "-",
            evt.SessionId ?? "-",
            evt.IpAddress ?? "-",
            evt.Details   ?? "-",
            evt.OccurredAt);
    }
}

/// <summary>
/// Extension method to register <see cref="ISecurityAuditLogger"/>.
/// Call from each service's composition root (APIs, BFF) after AddServiceDefaults.
/// </summary>
public static class SecurityAuditLoggerExtensions
{
    /// <summary>
    /// Registers <see cref="ISecurityAuditLogger"/> as a singleton backed by
    /// <see cref="SecurityAuditLogger"/>. Safe to call multiple times (idempotent).
    /// </summary>
    public static IServiceCollection AddSecurityAuditLogger(this IServiceCollection services)
    {
        services.AddSingleton<ISecurityAuditLogger, SecurityAuditLogger>();
        return services;
    }
}
