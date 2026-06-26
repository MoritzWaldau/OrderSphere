---
paths:
  - "src/Services/Advisory/OrderSphere.Mcp.Server/**"
---

MCP tool conventions:
- Tools in this server forward the caller's bearer token to downstream services via `BearerForwardingHandler`. Never hardcode credentials or use service-to-service auth here.
- User-scoped tools must call `UserToolGuard.AuthRequired` (or equivalent `ICallerContext` check) before accessing any user data.
- Registration: `builder.Services.AddMcpServer().WithHttpTransport().WithToolsFromAssembly()` — new tools are discovered automatically by assembly scan.
- Read an existing tool (e.g., `GetMyCartTool`) before writing a new one.

For the full bearer-forwarding pattern, see `.claude/skills/ordersphere-patterns/SKILL.md`.
