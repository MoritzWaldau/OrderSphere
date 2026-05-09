# OrderSphere — Session Context for LLMs

This file summarizes the state of the OrderSphere project after a major refactoring session. Read this **after** `CLAUDE.md` and `.github/copilot-instructions.md`. It documents what changed, why, and where to find things.

---

## Project Snapshot

**OrderSphere** is a Blazor Server e-commerce app on **.NET 10** following Clean Architecture + DDD + CQRS + MediatR. Stack:

- **UI**: Blazor Server with MudBlazor 9.4
- **Auth**: ASP.NET Core Identity (`ApplicationUser : IdentityUser`)
- **Persistence**: EF Core 10 + PostgreSQL (via `IdentityDbContext`)
- **Async messaging**: Azure Service Bus (run as emulator locally)
- **Background work**: Worker service consuming the `orders` queue
- **Orchestration**: .NET Aspire (`OrderSphere.AppHost`)
- **Email**: Azure Communication Email Service (`IEmailService`)
- **Tests**: xUnit + FluentAssertions 7.2 + NSubstitute (in `tests/`)

Solution file is `OrderSphere.slnx` (the new SLNX format, NOT a .sln). Build with `dotnet build OrderSphere.slnx`.

---

## Project Layout

```
src/
├── OrderSphere.AppHost/          # Aspire orchestrator
├── OrderSphere.ServiceDefaults/  # Shared OTel/health defaults
├── OrderSphere.Domain/           # Entities, ValueObjects, Errors, Primitives (Result<T>)
├── OrderSphere.Application/      # CQRS commands/queries via MediatR
├── OrderSphere.Infrastructure/   # EF Core, ServiceBus, Email, Identity adapters
├── OrderSphere.UI/OrderSphere.UI/  # Blazor Server (MudBlazor)
└── OrderSphere.Worker/           # BackgroundService for order processing

tests/
├── OrderSphere.Domain.Tests/        # xUnit + FluentAssertions
└── OrderSphere.Application.Tests/   # xUnit + FluentAssertions + NSubstitute
```

Note: Application's DI file is misspelled `DependecyInjection.cs` (existing typo, do not "fix" without checking references).

---

## Order Lifecycle (CRITICAL to understand)

The checkout flow is **asynchronous via Service Bus**:

```
User clicks Checkout (Blazor Server)
    ↓
CheckoutCartCommandHandler (synchronous)
    ├─ Validates cart (exists, not empty)
    ├─ Decrements product stock
    ├─ Deletes cart items
    ├─ Generates a CorrelationId (Guid.CreateVersion7)
    ├─ Publishes CheckoutCartEvent to "orders" queue
    └─ Returns Result<Guid>(correlationId)  ← NOT an OrderId

    ↓ (Service Bus "orders" queue)

OrderProcessor (BackgroundService in OrderSphere.Worker)
    ├─ Receives via ServiceBusProcessor (manual Complete/Abandon, MaxConcurrentCalls=1)
    ├─ Idempotency check by CorrelationId (CRITICAL — see below)
    ├─ Dispatches via MediatR to ProcessOrderCommandHandler
    └─ ProcessOrderCommandHandler:
        ├─ Creates Order entity
        ├─ Calls order.Confirm(TrackingNumberGenerator.Generate())
        │   → Sets Status = OrderStatus.Paid + TrackingNumber
        ├─ Persists Order
        └─ Best-effort: Look up customer email + send confirmation mail
```

**Order status enum** (`OrderStatus`): `Created → Paid → Shipped → Delivered` (or `Cancelled` from any non-terminal state).

**Status transitions enforced in domain entity** (`Order.cs`):
- `Confirm(trackingNumber)` — Created → Paid (called by Worker)
- `MarkShipped()` — Paid → Shipped (called by Admin)
- `MarkDelivered()` — Shipped → Delivered (called by Admin)
- `Cancel()` — anything except Delivered/Cancelled → Cancelled (called by Admin, restores stock)

Invalid transitions throw `InvalidOperationException`. Handlers catch and return `OrderErrors.InvalidStatusTransition`.

---

## Idempotency in Worker (subtle but important)

Service Bus is **at-least-once delivery**. The Worker MUST handle duplicate messages.

`ProcessOrderCommandHandler` (`src/OrderSphere.Application/Features/Order/ProcessOrder/ProcessOrderCommandHandler.cs`):

1. **Pre-check**: Query `Orders.Where(o => o.CorrelationId == evt.CorrelationId)`. If exists, return `Success(existingOrderId)` — don't insert.
2. **Race-condition catch**: Wrap insert in `try/catch (DbUpdateException dbEx) when (IsUniqueConstraintViolation(dbEx))` (Postgres SQLSTATE `23505`). On race, look up the winner and return its OrderId as success.
3. Worker treats Success → `CompleteMessageAsync` (correct behavior, even for duplicates).

`Order.CorrelationId` has a **unique index** in `OrderConfiguration.cs`. This is what enforces uniqueness.

---

## Migrations (NOT EnsureCreated)

The project switched from `dbContext.Database.EnsureCreated()` to **EF Core migrations** (`MigrateAsync`).

- Initial migration: `src/OrderSphere.Infrastructure/Migrations/20260509160752_InitialCreate.cs`
- `OrderSphere.UI` references `Microsoft.EntityFrameworkCore.Design` (PrivateAssets) — required for `dotnet ef` to find the startup project.
- `DataSeeder` (`src/OrderSphere.UI/OrderSphere.UI/Configuration/DataSeeder.cs`) is now **idempotent**: checks `Categories.AnyAsync()` and `userManager.FindByEmailAsync()` before seeding.

To create new migration:
```bash
dotnet ef migrations add MigrationName \
    -p src/OrderSphere.Infrastructure \
    -s src/OrderSphere.UI/OrderSphere.UI
```

`MigrateAsync()` runs at startup inside `DataSeeder.SeedDataAsync()` before any seeding.

---

## Service Bus Configuration

In `AppHost.cs`:

```csharp
var serviceBus = builder.AddAzureServiceBus("azure-service-bus")
    .RunAsEmulator(e => e.WithLifetime(ContainerLifetime.Persistent));

serviceBus.AddServiceBusQueue("orders")
    .WithProperties(cfg => cfg.MaxDeliveryCount = 10);

builder.AddProject<Projects.OrderSphere_UI>("ordersphere-ui")
    .WithReference(postgres).WithReference(serviceBus)
    .WaitFor(postgres).WaitFor(serviceBus);

builder.AddProject<Projects.OrderSphere_Worker>("ordersphere-worker")
    .WithReference(postgres).WithReference(serviceBus)
    .WaitFor(postgres).WaitFor(serviceBus);
```

**Important**: BOTH projects need `.WithReference(serviceBus)` AND `.WithReference(postgres)`. Aspire injects connection strings via these references. Without them you get `ConnectionStrings:azure-service-bus` is null errors at startup.

Worker registers `ServiceBusClient` via `builder.AddAzureServiceBusClient("azure-service-bus")` in its `Program.cs`. The string `"azure-service-bus"` MUST match the resource name in AppHost.

---

## Custom Abstractions (Application layer)

Application doesn't directly reference Identity or EF Core providers. These small abstractions bridge the gap (implementations in Infrastructure):

- **`IUserEmailLookup`** (`src/OrderSphere.Application/Abstraction/IUserEmailLookup.cs`)
  - Used by `ProcessOrderCommandHandler` to find customer email when sending order confirmation
  - Implementation: `UserEmailLookup` queries `OrderSphereDbContext.Users` directly (no UserManager dependency in Worker)

- **`IUserAdminService`** (`src/OrderSphere.Application/Abstraction/IUserAdminService.cs`)
  - Used by Admin Users page
  - Methods: `GetAllUsersAsync`, `GetAllRoleNamesAsync`, `AssignRoleAsync`, `RemoveRoleAsync`
  - Implementation: `UserAdminService` uses `UserManager<ApplicationUser>` + `RoleManager<IdentityRole>`

- **`IServiceBusPublisher`** + `ServiceBusPublisher` — publishes events to the `orders` queue.

- **`IEmailService`** has TWO methods:
  - `SendLinkAsync(toEmail, resetLink)` — original Identity-related (verification/reset)
  - `SendOrderConfirmationAsync(toEmail, OrderConfirmationData)` — new for orders

---

## CQRS Conventions

- Commands: `ICommand<TResponse>` returning `Task<TResponse>` typically `Result<T>` or `Result<bool>`.
- Queries: `IQuery<TResponse>` similarly.
- Handlers: `ICommandHandler<TC, TR>` / `IQueryHandler<TQ, TR>`.
- All handlers in `src/OrderSphere.Application/Features/<Aggregate>/<Action>/` — one folder per command/query.
- **Errors are values**, not exceptions. Use `Result<T>.Failure(SomeError)`. Errors live in `src/OrderSphere.Domain/Errors/`.

UI dispatches via `ISender` (MediatR). Injected as `[Inject] public required ISender Sender { get; set; }` on the base `OrderSphereComponentBase`.

---

## Admin Area (`/admin/*`)

Built in this session. All routes protected via `[Authorize(Roles = "Administrator")]`.

| Route | Page | Purpose |
|---|---|---|
| `/admin` | `AdminDashboard.razor` | Stats: total orders, revenue, pending shipments, low stock |
| `/admin/orders` | `AdminOrdersList.razor` | All orders with status filter |
| `/admin/orders/{id:guid}` | `AdminOrderDetail.razor` | Status actions: Ship/Deliver/Cancel |
| `/admin/products` | `AdminProductsList.razor` | List with edit/delete |
| `/admin/products/new` + `/{id:guid}` | `AdminProductForm.razor` | Combined create/edit form |
| `/admin/categories` | `AdminCategoriesList.razor` | List with edit/delete (delete blocked if products exist) |
| `/admin/categories/new` + `/{id:guid}` | `AdminCategoryForm.razor` | Combined create/edit |
| `/admin/users` | `AdminUsersList.razor` | List + role assign/remove |

**Layout**: `Components/Layouts/AdminLayout.razor` — own MudLayout with mini-drawer, NOT the public `MainLayout`.

**Navigation between admin and shop**: AdminLayout has "Zum Shop" button. There is currently **NO admin link in the public `MainLayout`/`Header`** — admins must navigate manually to `/admin`. This is a known gap.

**Confirmations**: Use JS `confirm()` via `IJSRuntime` instead of MudBlazor's `ShowMessageBox` (which had API friction in MudBlazor 9). Pattern:
```csharp
var confirmed = await JS.InvokeAsync<bool>("confirm", "Wirklich löschen?");
```

---

## Customer-Facing Pages (relevant changes)

- `/checkout/success` (`CheckoutSuccess.razor`) — Polls `GetOrderByCorrelationIdQuery` every 500ms for up to 5 seconds (10 attempts) waiting for the Worker to create the Order. Shows tracking number once available, or "wird verarbeitet" on timeout.

- `/account/orders` (`Orders.razor`) — Shows all customer orders with TrackingNumber. Each row is **clickable** (whole card is `@onclick`-able) → navigates to detail page.

- `/account/orders/{id:guid}` (`OrderDetail.razor`) — Customer's own order detail. Includes:
  - Tracking number with **Copy button** (uses `JS.InvokeVoidAsync("navigator.clipboard.writeText", ...)`)
  - Status timeline (MudTimeline) showing 4 steps
  - Mock shipping date (CreatedAt + 1d) and delivery estimate (+2 to +4d)
  - Mock tracking link `https://tracking.ordersphere.test/{trackingNumber}` (NOT a real URL)
  - Owner-authorization enforced in `GetOrderByIdQuery` handler

---

## Seeded Data (DataSeeder.cs)

**Categories**: Computer, Smartphones, Tablets, Accessories
**Products**: ~25 products across categories (Apple/Samsung electronics)

**Roles**: `Administrator`, `User`

**Users** (idempotent — only created if missing):
- `moritzwaldau99@gmail.com` / `!Admin123` → Administrator
- `e.tecklenborg@web.de` / `!User123` → User

---

## Known Gaps / Out of Scope

These were intentionally **NOT** implemented this session:

1. **No payment integration** — `PaymentMethod` enum exists but no Stripe/PayPal SDK
2. **Tracking is mocked** — `TrackingNumberGenerator` is `OS-{YYYY}-{8 hex chars}` (collision risk at scale, ~4B possibilities)
3. **No real shipping carrier integration** — tracking URL is a fake domain
4. **No admin link in public Header** — admins manually navigate to `/admin`
5. **No email on status change** — Worker sends mail on Order created (Paid). Status updates to Shipped/Delivered do NOT trigger emails.
6. **No outbox pattern** — if Service Bus publish succeeds but DB transaction commit fails (or vice versa), there's a small window of inconsistency. Currently mitigated by ordering: stock decrement is in same transaction as publish, but publish happens before commit, so a publish-then-rollback could leak the message. The handler's existing try/catch rolls back if publish throws.
7. **Application tests need expansion** — only 1 sample test exists. Most handlers are not covered. Would benefit from EF Core InMemory provider for integration-style handler tests.
8. **No rate limiting**, **no audit log for admin actions**, **no i18n** (German hardcoded), **no PDF invoices**

---

## Critical Coding Conventions

When extending this project, follow these patterns to stay consistent:

1. **One folder per Command/Query** in `Features/`. Two files: `XxxCommand.cs` and `XxxCommandHandler.cs`.
2. **Use `Result<T>`** — never throw for business logic failures. Errors are values defined in `OrderSphere.Domain/Errors/`.
3. **Domain methods enforce invariants**. Don't mutate entities from outside via property setters; expose intentional methods like `MarkShipped()`, `UpdateDetails(...)`.
4. **Auditable entities** auto-track `CreatedAt`, `UpdatedAt`, `IsDeleted` via `AuditSaveChangesInterceptor`. Soft deletes via `IsDeleted = true; query Where(!x.IsDeleted)`.
5. **Idempotent migrations**: when adding a new migration, also add idempotency to `DataSeeder` if you change seed data.
6. **Worker scope**: Always `scopeFactory.CreateAsyncScope()` per message — Background services are Singletons, MediatR/DbContext are Scoped.
7. **CSS classes from existing palette**: `.btn-pill`, `var(--mud-palette-*)`. No raw colors in styles.
8. **Razor pages**: use `@inherits OrderSphereComponentBase` + override `LoadDataAsync()`. Base class handles loading state and first-render data fetch.
9. **Service Bus messages**: serialize via `JsonSerializer.Serialize`. `args.Message.Body.ToObjectFromJson<T>()` to deserialize. The `CheckoutCartEvent` record uses positional args (CorrelationId, CheckoutCart, Items).

---

## Test Counts

After this session:
- `OrderSphere.Domain.Tests`: **26 passing tests** (Order lifecycle, Product, TrackingNumberGenerator)
- `OrderSphere.Application.Tests`: **3 passing tests** (CreateProductCommandHandler validation)

Run all: `dotnet test OrderSphere.slnx`

---

## How to Resume Work

If you're picking this up:

1. **First-time setup**: Drop the postgres container in Aspire dashboard (schema changed significantly). On next `dotnet run` in `src/OrderSphere.AppHost`, migrations auto-apply and seeders run.

2. **Common next tasks** (already discussed with user but not implemented):
   - Add admin link in `Components/Layouts/Header.razor` for users in role "Administrator"
   - Send email on Status → Shipped (extend `IEmailService` with `SendShippingNotificationAsync`)
   - Add EF Core InMemory tests for handlers
   - Implement Stripe Checkout for real payments
   - Implement Outbox pattern (transactional outbox table → background dispatcher)

3. **Code locations cheat sheet**:
   - Order lifecycle logic: `src/OrderSphere.Domain/Entities/Order.cs`
   - Worker entry point: `src/OrderSphere.Worker/Workers/OrderProcessor.cs`
   - Idempotency logic: `src/OrderSphere.Application/Features/Order/ProcessOrder/ProcessOrderCommandHandler.cs`
   - Polling on success page: `src/OrderSphere.UI/OrderSphere.UI/Components/Pages/Shopping/CheckoutSuccess.razor`
   - Admin order actions: `src/OrderSphere.UI/OrderSphere.UI/Components/Pages/Admin/Orders/AdminOrderDetail.razor.cs`
   - Dashboard query: `src/OrderSphere.Application/Features/Admin/GetDashboardStats/GetDashboardStatsQueryHandler.cs`

4. **Don't break these invariants**:
   - `Order.CorrelationId` unique index — enables idempotency. Don't remove.
   - `OrderProcessor` uses `MaxConcurrentCalls = 1` and `AutoCompleteMessages = false` — explicitly chosen.
   - Status transition enforcement is in domain entity. Handlers wrap calls in try/catch for `InvalidOperationException`.
   - `DataSeeder` MUST stay idempotent (check before insert).

---

## Build Commands

```bash
# Build all
dotnet build OrderSphere.slnx

# Run tests
dotnet test OrderSphere.slnx

# Run app (orchestrated by Aspire)
cd src/OrderSphere.AppHost
dotnet run

# Add migration
dotnet ef migrations add <Name> \
    -p src/OrderSphere.Infrastructure \
    -s src/OrderSphere.UI/OrderSphere.UI
```

---

**Generated**: end of session, after completing 3 implementation waves (Foundation/Idempotency, Admin Backend, Tests).
