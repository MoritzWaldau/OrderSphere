# Add MCP Tool

Scaffold a new tool in `OrderSphere.Mcp.Server` following the project's bearer-forwarding pattern.

## Usage

```
/add-mcp-tool <ToolClassName> <ServiceName>
```

- `<ToolClassName>` — PascalCase class name, e.g. `WishlistTools`
- `<ServiceName>` — the downstream service this tool calls, e.g. `Wishlist`

## What to build

1. **Read the canonical template first.** Open `src/Services/Advisory/OrderSphere.Mcp.Server/Tools/BasketTools.cs` to understand the required structure before generating any code.

2. **Create the tool class** at `src/Services/Advisory/OrderSphere.Mcp.Server/Tools/<ToolClassName>.cs`:
   - Annotate the class `[McpServerToolType]`, mark it `sealed`.
   - Each `[McpServerTool]` method must declare `ReadOnly`, `Idempotent`, `Destructive`, and `OpenWorld` — set them accurately based on what the tool does.
   - Every method must have a `[Description("...")]` that describes the tool's behavior from an AI-assistant perspective (what it returns, what it's useful for).
   - User-scoped tools (any tool accessing the caller's account) **must** inject `ICallerContext caller` and return `UserToolGuard.AuthRequired` when `!caller.HasBearerToken`. No exceptions.
   - Public catalog tools (no user data) skip the `ICallerContext` check.
   - Inject `IOrderSphereGateway gateway` for downstream calls — never `HttpClient` directly.
   - Return JSON strings via `JsonSerializer.Serialize(..., Json)` where `Json = new JsonSerializerOptions(JsonSerializerDefaults.Web)`.

3. **Add gateway method(s)** if the tool needs a downstream endpoint not already on `IOrderSphereGateway`:
   - Declare the method on `IOrderSphereGateway` in `Gateway/OrderSphereGateway.cs`.
   - Implement it in `OrderSphereGateway` using the existing `HttpClient` field (`http`).
   - Add the matching DTO(s) to `Gateway/GatewayDtos.cs` (records only, no classes).
   - Derive the BFF URL path from the existing entries in `OrderSphereGateway` — the base address is already configured in `Program.cs`.

4. **No registration needed.** `WithToolsFromAssembly()` in `Program.cs` discovers the class automatically.

5. **Verify** that `dotnet build src/Services/Advisory/OrderSphere.Mcp.Server` succeeds before reporting done.

## Constraints

- `BearerForwardingHandler` is registered globally — all `IOrderSphereGateway` calls carry the caller's token automatically. Do not re-attach headers manually.
- No direct references to other service projects (`Ordering.Application`, etc.) — all data goes through `IOrderSphereGateway`.
- Write tools (non-idempotent, destructive) require explicit user confirmation in the tool description and should set `ReadOnly = false, Destructive = true`.
