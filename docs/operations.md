# Operations & Observability Runbook

How to observe and operate OrderSphere. The cross-cutting wiring lives in
`src/Hosting/OrderSphere.ServiceDefaults` (`Extensions.cs`), applied by every service and gateway.
Deployment is covered separately in [deploy-ordersphere.md](deploy-ordersphere.md).

## Endpoints exposed by every service

Mapped by `MapDefaultEndpoints` (`ServiceDefaults/Extensions.cs`):

| Path | Purpose | Notes |
|---|---|---|
| `/health` | Readiness — **all** registered health checks must pass. | Includes the PostgreSQL (`postgres`, tagged `ready`,`db`) check and Aspire-registered Redis/Service Bus checks. |
| `/alive` | Liveness — only checks tagged `live`. | The built-in `self` check; use for liveness probes. |
| `/version` | Reports service name and compiled version. | From `AssemblyInformationalVersion` (`VersionPrefix` in `Directory.Build.props`). |

> Health endpoints are mapped for all environments. Use `/alive` for liveness and `/health` for
> readiness probes in Container Apps.

## Telemetry (OpenTelemetry)

Configured in `ServiceDefaults` (`AddOpenTelemetry`):

- **Traces:** ASP.NET Core and HttpClient instrumentation on every service, plus:
  - `OrderSphere.EventBus` — publish and consume spans across every Service Bus queue. W3C
    `traceparent` is persisted on the Outbox row and injected into the message, so the async Outbox
    dispatch is linked to the originating request trace.
  - `OrderSphere.Application` — one span per MediatR handler (CQRS command/query), tagged with
    `request.outcome` and `error.code`.
  - `{service.name}` — the Aspire application name; used for GenAI spans in the advisory agent.
  - EF Core / Npgsql DB spans are provided automatically by Aspire's `AddNpgsqlDbContext`.
- **Metrics:** ASP.NET Core, HttpClient, .NET runtime, plus the `OrderSphere` meter (all custom
  business counters and histograms — see [Business metrics](#business-metrics)).
- **Logs:** OpenTelemetry logging with formatted messages and scopes. EF Core SQL command logs are
  silenced at `Warning` by default (raise the `Microsoft.EntityFrameworkCore.Database.Command`
  category per environment where raw SQL is needed).

### Resource attributes

Every signal carries `service.name` (Aspire app name), `service.version` (from
`AssemblyInformationalVersion`), `service.instance.id` (machine name), and
`deployment.environment` (ASP.NET environment name). These are the primary filter axes in the
Aspire dashboard and in Application Insights (`cloud_RoleName` = service.name).

### Sampling

Controlled by `OpenTelemetry:TracesSampleRatio` (double, default `1.0`). Uses
`ParentBasedSampler(TraceIdRatioBasedSampler)` — the parent's sampling decision is respected for
child spans so a trace is either fully sampled or fully dropped. Azure Monitor applies its own
additional sampling via `APPLICATIONINSIGHTS_SAMPLING_PERCENTAGE`. In production, set
`OpenTelemetry:TracesSampleRatio` to `0.1`–`0.2` and `APPLICATIONINSIGHTS_SAMPLING_PERCENTAGE`
to `10`–`20` depending on volume and cost targets.

### PII policy

Log messages must not include personally identifiable information. Enforced by:

- `DomainEventLoggingHandler` — logs event type only, never the event payload.
- `LoggingNotificationEmailService` — masks email addresses (`a***@domain.com`).
- All new log statements must follow the same rule: log IDs and types, not customer data.

### Where telemetry goes

Export is selected by configuration:

- **Local / OTLP:** when `OTEL_EXPORTER_OTLP_ENDPOINT` is set (Aspire sets this automatically), the
  OTLP exporter is used and all signals appear in the **Aspire dashboard** (traces, structured logs,
  metrics, resource health). The dashboard URL is printed on startup.
- **Azure:** when `APPLICATIONINSIGHTS_CONNECTION_STRING` is present, `UseAzureMonitor()` exports to
  Application Insights instead.

## Resilience and service discovery

- All `HttpClient` instances get `AddStandardResilienceHandler()` (timeouts, retries, circuit
  breaker) by default — relevant when reading traces that show retried cross-service calls.
- Service-to-service URLs resolve through `AddServiceDiscovery()`; logical names (e.g.
  `ordersphere-catalog`) are resolved by Aspire, not hardcoded.

## Operational tasks

| Task | How |
|---|---|
| Check a service is up | `GET /alive`; for readiness incl. dependencies `GET /health`. |
| Identify the running version | `GET /version`. |
| Trace a request across services | Open the Aspire dashboard → Traces; follow the trace id across BFF → gateway → services. The identity-forwarding chain is described in [architecture.md](architecture.md#identity-forwarding). |
| Inspect a chat turn | Filter traces by source `OrderSphere.Advisory.Agent`; one span per model round-trip, plus the MCP tool calls it triggers. |
| Schema migration | Each service runs `Database.Migrate()` on startup; no separate step. Migration commands per service: [architecture.md](architecture.md#ef-migrations). |

## Business metrics

All custom metrics share the meter name `OrderSphere` and are prefixed `ordersphere.*`. They flow
into `customMetrics` in Application Insights.

| Metric name | Type | Tags | Emitted by |
|---|---|---|---|
| `ordersphere.orders.placed` | Counter | — | Ordering.Worker (OrderProcessor) |
| `ordersphere.orders.confirmed` | Counter | — | Ordering.Worker (PaymentResultProcessor) |
| `ordersphere.orders.cancelled` | Counter | — | Ordering.Worker (PaymentResultProcessor) |
| `ordersphere.payments.processed` | Counter | `provider`, `succeeded` | Payment.Worker |
| `ordersphere.payment.duration` | Histogram (ms) | — | Payment.Worker |
| `ordersphere.webhook.dispatched` | Counter | — | Webhooks.Worker |
| `ordersphere.webhook.failed` | Counter | — | Webhooks.Worker |
| `ordersphere.outbox.published` | Counter | — | OutboxDispatcher (all services) |
| `ordersphere.outbox.poison` | Counter | — | OutboxDispatcher (all services) |
| `ordersphere.catalog.cache.hits` | Counter | — | Catalog.Application (GetProductBySlug) |
| `ordersphere.catalog.cache.misses` | Counter | — | Catalog.Application (GetProductBySlug) |
| `ordersphere.mediatr.request.duration` | Histogram (ms) | `request`, `outcome` | LoggingBehavior (all services) |

---

## Azure Monitor

Applies when `APPLICATIONINSIGHTS_CONNECTION_STRING` is set. The Application Insights workspace
receives all three signal types (traces, metrics, logs) via `UseAzureMonitor()`.

### Workbook: Service Health

Paste each KQL snippet into an Application Insights workbook tab.

**Request rate per service (1-minute buckets)**

```kusto
requests
| where timestamp > ago(1h)
| summarize RequestsPerMin = count() by bin(timestamp, 1m), cloud_RoleName
| render timechart
```

**Error rate per service (5-minute buckets)**

```kusto
requests
| where timestamp > ago(1h)
| summarize
    Total = count(),
    Failed = countif(success == false)
  by bin(timestamp, 5m), cloud_RoleName
| extend ErrorRatePct = iff(Total == 0, 0.0, todouble(Failed) / todouble(Total) * 100)
| project timestamp, cloud_RoleName, ErrorRatePct
| render timechart
```

**HTTP latency percentiles (P50 / P95 / P99)**

```kusto
requests
| where timestamp > ago(1h)
| summarize
    P50 = percentile(duration, 50),
    P95 = percentile(duration, 95),
    P99 = percentile(duration, 99)
  by bin(timestamp, 5m), cloud_RoleName
| render timechart
```

**Dependency failures (DB, Service Bus, outgoing HTTP)**

```kusto
dependencies
| where timestamp > ago(1h)
| where success == false
| summarize Failures = count() by bin(timestamp, 5m), cloud_RoleName, type, target
| order by Failures desc
```

---

### Workbook: Business

**Order funnel (placed → confirmed / cancelled)**

```kusto
customMetrics
| where timestamp > ago(24h)
| where name in (
    "ordersphere.orders.placed",
    "ordersphere.orders.confirmed",
    "ordersphere.orders.cancelled")
| summarize Total = sum(value) by bin(timestamp, 10m), name
| render timechart
```

**Payment success rate by provider**

```kusto
customMetrics
| where timestamp > ago(24h)
| where name == "ordersphere.payments.processed"
| extend Provider = tostring(customDimensions["provider"])
| extend Succeeded = tostring(customDimensions["succeeded"])
| summarize Count = sum(value) by bin(timestamp, 10m), Provider, Succeeded
| render timechart
```

**Average payment processing duration**

```kusto
customMetrics
| where timestamp > ago(24h)
| where name == "ordersphere.payment.duration"
| summarize AvgMs = sum(valueSum) / sum(valueCount) by bin(timestamp, 5m)
| render timechart
```

**Webhook failure rate**

```kusto
customMetrics
| where timestamp > ago(24h)
| where name in ("ordersphere.webhook.dispatched", "ordersphere.webhook.failed")
| summarize Total = sum(value) by bin(timestamp, 10m), name
| render timechart
```

**Outbox poison message count**

```kusto
customMetrics
| where timestamp > ago(24h)
| where name == "ordersphere.outbox.poison"
| summarize Poison = sum(value) by bin(timestamp, 10m)
| render timechart
```

**Catalog cache hit ratio**

```kusto
customMetrics
| where timestamp > ago(1h)
| where name in ("ordersphere.catalog.cache.hits", "ordersphere.catalog.cache.misses")
| summarize Total = sum(value) by bin(timestamp, 5m), name
| evaluate pivot(name, sum(Total))
| extend HitRatioPct = iff(
    column_ifexists("ordersphere.catalog.cache.hits", 0.0) + column_ifexists("ordersphere.catalog.cache.misses", 0.0) == 0,
    0.0,
    column_ifexists("ordersphere.catalog.cache.hits", 0.0) /
      (column_ifexists("ordersphere.catalog.cache.hits", 0.0) + column_ifexists("ordersphere.catalog.cache.misses", 0.0)) * 100)
| project timestamp, HitRatioPct
| render timechart
```

---

### Workbook: End-to-End Trace

Requires Phase 1 distributed tracing (W3C traceparent propagation across Service Bus). All hops of
a checkout flow share one `operation_Id`.

**All spans for a single trace**

```kusto
union requests, dependencies
| where operation_Id == "<paste-trace-id>"
| project
    timestamp,
    itemType,
    name,
    DurationMs = duration,
    success,
    Service = cloud_RoleName,
    ParentId = operation_ParentId
| order by timestamp asc
```

**Logs correlated to a trace**

```kusto
traces
| where operation_Id == "<paste-trace-id>"
| project timestamp, message, severityLevel, cloud_RoleName
| order by timestamp asc
```

**Identify slow operations in a trace**

```kusto
union requests, dependencies
| where operation_Id == "<paste-trace-id>"
| where duration > 500
| project timestamp, name, DurationMs = duration, cloud_RoleName
| order by DurationMs desc
```

---

### Alert rules

Create these as Azure Monitor metric or log alert rules. Recommended evaluation window and
frequency are noted per rule; adjust thresholds to baseline traffic.

| Rule | Signal | Condition | Window | Frequency |
|---|---|---|---|---|
| High error rate | Log alert on `requests` | `countif(success==false)/count() > 0.05` per `cloud_RoleName` | 5 min | 1 min |
| P95 latency | Log alert on `requests` | `percentile(duration,95) > 2000` per `cloud_RoleName` | 5 min | 1 min |
| Outbox poison | Metric alert on `customMetrics` name=`ordersphere.outbox.poison` | `sum > 0` | 5 min | 1 min |
| Service Bus DLQ | Platform metric `DeadLetteredMessageCount` on Service Bus queue | `> 0` per queue | 15 min | 5 min |
| Health probe failure | Metric alert on `availabilityResults` | `availabilityPercentage < 100` | 5 min | 1 min |
| Worker health down | Log alert on `requests` path `/alive` | `countif(resultCode!="200") > 0` per `cloud_RoleName` | 5 min | 1 min |

**KQL for the high-error-rate log alert:**

```kusto
requests
| where timestamp > ago(5m)
| summarize
    Total = count(),
    Failed = countif(success == false)
  by cloud_RoleName
| where Total > 10
| extend ErrorRatePct = todouble(Failed) / todouble(Total) * 100
| where ErrorRatePct > 5
```

**KQL for the outbox poison log alert (fallback if metric alert not available):**

```kusto
customMetrics
| where timestamp > ago(5m)
| where name == "ordersphere.outbox.poison"
| summarize PoisonCount = sum(value)
| where PoisonCount > 0
```

---

## Deployment-time troubleshooting

First-deploy issues (Key Vault soft-delete, ACR credentials, resource-group mismatch, Redis Entra
auth) are catalogued in the troubleshooting table of
[deploy-ordersphere.md](deploy-ordersphere.md#troubleshooting-actually-encountered-on-the-first-deploy).
This runbook does not duplicate it.
