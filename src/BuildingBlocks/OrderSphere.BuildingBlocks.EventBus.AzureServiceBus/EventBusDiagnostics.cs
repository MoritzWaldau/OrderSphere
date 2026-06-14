using System.Diagnostics;
using Azure.Messaging.ServiceBus;

namespace OrderSphere.BuildingBlocks.EventBus.AzureServiceBus;

/// <summary>
/// Distributed-tracing helpers for the Service Bus event pipeline. Propagates the W3C trace
/// context across the asynchronous outbox → queue → consumer boundary so that a single trace
/// spans every hop (HTTP request → outbox → publish → consume → outbox → …).
///
/// Uses only the BCL <see cref="ActivitySource"/>/<see cref="Activity"/> API (no OpenTelemetry
/// package dependency). The source is registered in ServiceDefaults via
/// <c>tracing.AddSource(EventBusDiagnostics.SourceName)</c>.
/// </summary>
public static class EventBusDiagnostics
{
    /// <summary>ActivitySource name. Register this in the tracer provider to record the spans.</summary>
    public const string SourceName = "OrderSphere.EventBus";

    public static readonly ActivitySource Source = new(SourceName);

    /// <summary>W3C trace-context header carried as a Service Bus application property.</summary>
    private const string TraceParentProperty = "traceparent";

    // The OutboxDispatcher publishes on a timer, long after the originating request/consume
    // completed, so the original context is no longer on Activity.Current. It is persisted on the
    // outbox row and restored here as an ambient parent for the publish span.
    private static readonly AsyncLocal<ActivityContext?> AmbientPublishParent = new();

    /// <summary>
    /// Restores the originating trace context (captured when the outbox row was written) so the
    /// publish span and everything downstream join the original trace. Dispose to clear it.
    /// </summary>
    public static IDisposable RestorePublishParent(string? traceParent)
    {
        var previous = AmbientPublishParent.Value;
        AmbientPublishParent.Value =
            ActivityContext.TryParse(traceParent, null, isRemote: true, out var ctx) ? ctx : null;
        return new ParentScope(previous);
    }

    /// <summary>Starts a producer span for a publish to <paramref name="destination"/>.</summary>
    public static Activity? StartPublish(string destination, string? messageId)
    {
        var activity = AmbientPublishParent.Value is { } parent
            ? Source.StartActivity($"{destination} publish", ActivityKind.Producer, parent)
            : Source.StartActivity($"{destination} publish", ActivityKind.Producer);

        SetMessagingTags(activity, destination, "publish", messageId);
        return activity;
    }

    /// <summary>Writes the current trace context onto the outgoing message.</summary>
    public static void Inject(ServiceBusMessage message)
    {
        // Prefer the active producer span; fall back to the restored ambient parent so the trace
        // id still propagates when the source is not sampled (StartPublish returned null).
        var traceParent = Activity.Current?.Id ?? FormatTraceParent(AmbientPublishParent.Value);
        if (traceParent is not null)
            message.ApplicationProperties[TraceParentProperty] = traceParent;
    }

    /// <summary>Starts a consumer span linked to the producer context carried by the message.</summary>
    public static Activity? StartProcess(ServiceBusReceivedMessage message, string queueName)
    {
        Activity? activity =
            message.ApplicationProperties.TryGetValue(TraceParentProperty, out var raw)
            && raw is string traceParent
            && ActivityContext.TryParse(traceParent, null, isRemote: true, out var ctx)
                ? Source.StartActivity($"{queueName} process", ActivityKind.Consumer, ctx)
                : Source.StartActivity($"{queueName} process", ActivityKind.Consumer);

        SetMessagingTags(activity, queueName, "process", message.MessageId);
        return activity;
    }

    private static void SetMessagingTags(Activity? activity, string destination, string operation, string? messageId)
    {
        if (activity is null)
            return;

        activity.SetTag("messaging.system", "servicebus");
        activity.SetTag("messaging.destination.name", destination);
        activity.SetTag("messaging.operation", operation);
        if (messageId is not null)
            activity.SetTag("messaging.message.id", messageId);
    }

    private static string? FormatTraceParent(ActivityContext? context)
    {
        if (context is not { } ctx)
            return null;

        var sampled = (ctx.TraceFlags & ActivityTraceFlags.Recorded) != 0 ? "01" : "00";
        return $"00-{ctx.TraceId}-{ctx.SpanId}-{sampled}";
    }

    private sealed class ParentScope(ActivityContext? previous) : IDisposable
    {
        public void Dispose() => AmbientPublishParent.Value = previous;
    }
}
