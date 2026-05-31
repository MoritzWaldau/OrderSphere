namespace OrderSphere.BuildingBlocks.Security;

/// <summary>
/// Emits security-relevant events to the structured audit log.
/// Implementations write to the standard ILogger pipeline; the
/// OpenTelemetry exporter (ServiceDefaults) forwards the entries
/// to the configured telemetry sink.
/// </summary>
public interface ISecurityAuditLogger
{
    void Log(SecurityAuditEvent evt);
}

/// <summary>
/// An immutable record describing one security-relevant occurrence.
/// All fields except <see cref="Type"/> are optional so callers include
/// only the context that is available at the call site.
/// </summary>
public sealed record SecurityAuditEvent(
    SecurityAuditEventType Type,
    string? UserId      = null,
    string? SessionId   = null,
    string? IpAddress   = null,
    string? Details     = null)
{
    /// <summary>UTC timestamp of the event. Defaults to now if not supplied.</summary>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public enum SecurityAuditEventType
{
    LoginSuccess,
    LoginFailure,
    LogoutInitiated,
    BackchannelLogoutReceived,
    BackchannelLogoutRevoked,
    RefreshTokenRotated,
    RefreshTokenRevoked,
    AuthorizationDenied,
    TokenValidationFailed,
    AntiforgeryValidationFailed,
}
