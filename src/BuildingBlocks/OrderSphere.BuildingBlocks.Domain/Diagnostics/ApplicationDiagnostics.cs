using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace OrderSphere.BuildingBlocks.Diagnostics;

/// <summary>
/// Shared diagnostics primitives. The meter name <see cref="MeterName"/> ("OrderSphere") is the
/// single registration handle for every custom OrderSphere metric — registered once in
/// ServiceDefaults via <c>metrics.AddMeter("OrderSphere")</c>. The activity source surfaces a
/// span per MediatR request (CQRS handler) and is registered via
/// <c>tracing.AddSource("OrderSphere.Application")</c>.
/// </summary>
public static class ApplicationDiagnostics
{
    public const string MeterName = "OrderSphere";
    public const string ActivitySourceName = "OrderSphere.Application";

    public static readonly Meter Meter = new(MeterName);
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    /// <summary>MediatR request handler duration in milliseconds, tagged by request and outcome.</summary>
    public static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "ordersphere.mediatr.request.duration",
        unit: "ms",
        description: "MediatR request handler duration.");

    /// <summary>Wall-clock duration of a model-backed advisory turn, tagged by outcome (completed|canceled|failed).</summary>
    public static readonly Histogram<double> AdvisorTurnDuration = Meter.CreateHistogram<double>(
        "ordersphere.advisor.turn.duration",
        unit: "ms",
        description: "Wall-clock duration of a model-backed advisory turn.");

    /// <summary>Model tokens consumed by the advisory agent, tagged by direction (input|output).</summary>
    public static readonly Counter<long> AdvisorTokens = Meter.CreateCounter<long>(
        "ordersphere.advisor.tokens",
        unit: "{token}",
        description: "Model tokens consumed by the advisory agent, tagged by direction.");

    /// <summary>Advisory agent tool invocations, tagged by tool and outcome (success|error).</summary>
    public static readonly Counter<long> AdvisorToolInvocations = Meter.CreateCounter<long>(
        "ordersphere.advisor.tool.invocations",
        description: "Advisory agent tool invocations, tagged by tool and outcome.");
}
