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

- **Traces:** ASP.NET Core and HttpClient instrumentation on every service. The advisory agent adds a
  GenAI span per model round-trip (source `OrderSphere.Advisory.Agent`).
- **Metrics:** ASP.NET Core, HttpClient, and .NET runtime instrumentation.
- **Logs:** OpenTelemetry logging with formatted messages and scopes.

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

## Deployment-time troubleshooting

First-deploy issues (Key Vault soft-delete, ACR credentials, resource-group mismatch, Redis Entra
auth) are catalogued in the troubleshooting table of
[deploy-ordersphere.md](deploy-ordersphere.md#troubleshooting-actually-encountered-on-the-first-deploy).
This runbook does not duplicate it.
