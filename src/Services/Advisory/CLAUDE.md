# Advisory Service

## Project layout

This service has **5 projects**, not the standard 4:

| Project | Role |
|---|---|
| `OrderSphere.Advisory.Api` | HTTP endpoints + agent host |
| `OrderSphere.Advisory.Application` | CQRS handlers, queries, DTOs |
| `OrderSphere.Advisory.Domain` | Entities, domain errors |
| `OrderSphere.Advisory.Infrastructure` | EF DbContext, configurations, migrations |
| `OrderSphere.Mcp.Server` | MCP tool host — peer project, not a Clean Architecture layer |

`OrderSphere.Mcp.Server` sits outside the dependency cone. It references `Advisory.Application` abstractions and calls downstream services through `IOrderSphereGateway`, but it does not reference `Advisory.Infrastructure` and has no `DbContext`.

## Adding an MCP tool

Tools live in `OrderSphere.Mcp.Server/Tools/`. Copy an existing tool as the template.

The tool set is assembled per-request in `Advisory.Api/Agent/AdvisorToolSource.cs` — no manual registration is required when a new tool class is added.

**Required attribute on every tool:**

```csharp
[McpServerTool(ReadOnly = true, Destructive = false)]
```

A write tool is a deliberate architecture exception. It must include an explicit human-in-the-loop confirmation step — do not add a silent write tool.

## Bearer forwarding

`BearerForwardingHandler` is registered on the `HttpClient` backing `IOrderSphereGateway`. It captures the caller's JWT and forwards it on every outbound request to the API Gateway. Downstream services derive the customer identity from that token.

Do not bypass this handler and do not pass credentials any other way.

## User-scope guard

Call `UserToolGuard.AuthRequired(context)` at the top of any tool that requires an authenticated user. Tools that omit this call are reachable anonymously.

## No direct database access

All data reads from the MCP server go through `IOrderSphereGateway` → API Gateway → downstream service. The MCP server has no `DbContext` and must not acquire one.
