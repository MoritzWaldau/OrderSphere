# Claude Code Instructions for OrderSphere

This document contains guidelines for Claude when working on the OrderSphere project. These instructions complement the design guide in `.github/copilot-instructions.md`.

## 🏛️ Architecture & Code Patterns

### Clean Architecture Layers
```
UI (Blazor Server)
  ↓
Application (CQRS with MediatR)
  ├── Commands (write operations)
  ├── Queries (read operations)
  ├── Handlers (ICommandHandler, IQueryHandler)
  └── DTOs (Data Transfer Objects)
  ↓
Infrastructure (External Services)
  ├── Persistence (EF Core, PostgreSQL)
  ├── Email Service (Azure Communication)
  ├── Service Bus (Azure Service Bus)
  └── Interceptors (Audit, etc.)
  ↓
Domain (DDD - Business Logic)
  ├── Entities (Order, Product, Cart)
  ├── Value Objects (Address)
  ├── Events (Domain Events)
  └── Primitives (Result<T>, Error)
```

### Design Patterns Used
- **CQRS**: Commands for writes, Queries for reads (via MediatR)
- **DDD**: Rich domain models with invariants and business rules
- **Result Pattern**: Functional error handling with `Result<T>` (not exceptions for business logic)
- **Entity Auditing**: Automatic `CreatedAt`, `UpdatedAt`, `IsDeleted` tracking via `AuditableEntity`
- **Railway-Oriented Programming**: Error propagation without exceptions
- **Service Bus Events**: Asynchronous event publishing for order processing

## 📋 Code Style & Conventions

### C# Standards
- **Framework**: .NET 10.0 with nullable reference types enabled (`#nullable enable`)
- **Language Features**: C# 13.0 implicit usings enabled
- **Async-First**: All I/O operations must be `async`/`await` (no `.Result`, no `.Wait()`)
- **Naming**: PascalCase for public members, camelCase for local variables
- **Records vs Classes**: Use `record` for immutable DTOs, `class` for mutable entities

### Error Handling
**DO:**
```csharp
// Return Result<T> for domain operations
public async Task<Result<OrderDto>> CreateOrderAsync(CreateOrderCommand command)
{
    if (cart == null)
        return OrderErrors.CartNotFoundError;
    
    var order = Order.Create(customerId, items, shippingAddress);
    return Result.Success(mapper.Map<OrderDto>(order));
}
```

**DON'T:**
```csharp
// Don't throw exceptions for business logic validation
throw new InvalidOperationException("Cart not found");
```

### Entity & Value Object Rules
- **Entities** inherit from `AuditableEntity` (provides Id, CreatedAt, UpdatedAt, IsDeleted)
- **Value Objects** are immutable and compare by value (override `==`, `!=`, `GetHashCode()`)
- **Domain Events** go in `src/OrderSphere.Domain/Events/` and extend `INotification` (MediatR)
- **Enums** go in `src/OrderSphere.Domain/Enums/`

### MediatR Commands & Queries
**Commands** (write operations):
```csharp
public sealed record AddToCartCommand(Guid CustomerId, Guid ProductId, int Quantity)
    : ICommand<Result<CartDto>>;

public sealed class AddToCartCommandHandler
    : ICommandHandler<AddToCartCommand, Result<CartDto>>
{
    private readonly IDbContext _dbContext;
    
    public async Task<Result<CartDto>> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

**Queries** (read operations):
```csharp
public sealed record GetProductQuery(Guid ProductId)
    : IQuery<Result<ProductDto>>;

public sealed class GetProductQueryHandler
    : IQueryHandler<GetProductQuery, Result<ProductDto>>
{
    // Implementation
}
```

## 🎨 UI Development (Blazor & MudBlazor)

### Design Philosophy
See `.github/copilot-instructions.md` for comprehensive styling guide. Key principles:
- Inspired by Apple.com and Amazon — clean contrasts, generous whitespace, subtle shadows
- No UPPERCASE text, no hard edges
- `border-radius: 12px` global default, `16px` for cards, `20px` for auth cards, `100px` for buttons
- Use MUD palette variables: `var(--mud-palette-*)` for dark mode compatibility
- Pill buttons: `.btn-pill`, `.btn-pill-white`, `.btn-pill-outline-white`

### Component Structure
```
src/OrderSphere.UI/OrderSphere.UI/
├── Components/
│   ├── Layouts/              # MainLayout, Header, Footer, MobileDrawer
│   ├── Pages/                # Product, Cart, Checkout, Account pages
│   └── [Features]/           # Component subdirectories by feature
├── Services/                 # CartService, CurrentUserService, etc.
├── App.razor                 # Root component
├── Program.cs                # Startup configuration
└── app.css                   # Custom CSS (see copilot-instructions.md)
```

### Blazor Best Practices
- Use `InteractiveServer` render mode for full interactivity
- Leverage MudBlazor components over HTML
- Implement `OnParametersSetAsync` for cascading parameters
- Use `EventCallback<T>` for child-to-parent communication
- Prefer `@rendermode InteractiveServer` in component files

### MudBlazor Usage
- `Elevation="0"` + Border for modern look (not shadows)
- Icons: `Icons.Material.Outlined.*` in headers (lighter than Filled)
- Colors: Use MUD palette tokens `Color.Primary`, `Color.Secondary`, etc.
- Spacing: `pa-4` (padding all), `mb-2` (margin bottom), `gap-4` (gap)
- Typography: `Typo.H3`, `Typo.Body1`, `Typo.Subtitle2`, etc.

## 🔗 Database & Entity Framework

### DbContext Pattern
- `OrderSphereDbContext` in `src/OrderSphere.Infrastructure/Persistence/`
- All entity configurations via `ApplyConfigurationsFromAssembly()` in DbContext constructor
- Implement transaction management: `BeginTransactionAsync()`, `CommitAsync()`, `RollbackAsync()`
- Use soft deletes: check `!x.IsDeleted` in queries

### Entity Configuration Files
Location: `src/OrderSphere.Infrastructure/EntityConfigurations/`

Example:
```csharp
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        
        builder
            .OwnsOne(o => o.ShippingAddress, navBuilder =>
            {
                navBuilder.WithOwner();
            });
        
        builder
            .HasMany(o => o.Items)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
        
        builder
            .Property(o => o.Status)
            .HasConversion<string>();
    }
}
```

### Migrations
```bash
# Create migration
dotnet ef migrations add MigrationName \
    -p src/OrderSphere.Infrastructure \
    -s src/OrderSphere.UI

# Apply migrations
dotnet ef database update \
    -p src/OrderSphere.Infrastructure \
    -s src/OrderSphere.UI
```

## 📦 External Services

### Azure Email Service
- **Location**: `src/OrderSphere.Infrastructure/Email/EmailService.cs`
- **Configuration**: `MailServiceConfiguration` in `appsettings.json`
- **Usage**: Send verification emails, password reset links
- **Pattern**: Implement `IEmailService` interface for dependency injection

### Azure Service Bus
- **Location**: `src/OrderSphere.Infrastructure/ServiceBus/ServiceBusPublisher.cs`
- **Queue**: `orders` (for `CheckoutCartEvent`)
- **Pattern**: Publish events for asynchronous processing
- **Example**:
```csharp
public async Task PublishCheckoutCartEventAsync(CheckoutCartEvent checkoutCartEvent)
{
    var message = new ServiceBusMessage(JsonSerializer.Serialize(checkoutCartEvent))
    {
        MessageId = Guid.NewGuid().ToString()
    };
    await _client.SendMessageAsync(message);
}
```

## 🧪 Testing

### Test Location
- Unit/Integration tests should go in a `tests/` directory parallel to `src/`
- Test naming: `[FeatureName]Tests.cs`
- Test method naming: `[Method]_[Scenario]_[Expected]`

### Test Patterns
```csharp
[Fact]
public async Task AddToCart_WithValidProduct_ReturnsSuccessResult()
{
    // Arrange
    var command = new AddToCartCommand(customerId, productId, quantity);
    
    // Act
    var result = await handler.Handle(command, default);
    
    // Assert
    Assert.True(result.IsSuccess);
}
```

## 🚀 Running & Debugging

### Development
```bash
# With .NET Aspire (orchestration)
cd src/OrderSphere.AppHost
dotnet run

# OR direct run (ensure PostgreSQL & Service Bus running)
cd src/OrderSphere.UI
dotnet run

# With hot-reload
dotnet watch run
```

### Configuration Files
- `appsettings.json` — Local development config
- `appsettings.Development.json` — Dev-specific overrides
- `.github/copilot-instructions.md` — UI/Design guidelines
- `Directory.Packages.props` — Centralized NuGet version management

## 📝 Commit Message Guidelines

Follow conventional commits:
```
feat: Add product filtering by category
fix: Resolve cart item quantity validation bug
refactor: Extract payment logic to domain service
docs: Update README with deployment guide
style: Format MudBlazor component props
test: Add integration tests for checkout flow
```

## 🔍 Code Review Checklist

Before creating a PR:
- ✅ All async operations use `await` (no `.Result`)
- ✅ Error handling uses `Result<T>` pattern
- ✅ Entities inherit from `AuditableEntity`
- ✅ MediatR handlers implement correct interface
- ✅ UI components follow MudBlazor & design guide
- ✅ No hardcoded colors (use `var(--mud-palette-*)`)
- ✅ Database changes have migrations
- ✅ Soft deletes: queries check `!x.IsDeleted`
- ✅ No blocking calls (`.Result`, `.Wait()`)

## 📚 Key Files Reference

| File | Purpose |
|------|---------|
| `src/OrderSphere.Domain/Primitives/Result.cs` | Result<T> pattern implementation |
| `src/OrderSphere.Domain/Abstraction/ICommand.cs` | CQRS command interface |
| `src/OrderSphere.Domain/Abstraction/IQuery.cs` | CQRS query interface |
| `src/OrderSphere.Infrastructure/Persistence/OrderSphereDbContext.cs` | Entity Framework context |
| `src/OrderSphere.UI/OrderSphere.UI/Components/Layouts/MainLayout.razor` | Main UI layout |
| `src/OrderSphere.UI/OrderSphere.UI/Services/CartService.cs` | Client-side cart state |
| `.github/copilot-instructions.md` | Design & styling guide |

## 💡 When to Ask for Clarification

Ask the user before:
- Adding new external dependencies (NuGet packages)
- Changing database schema significantly
- Introducing new architectural patterns
- Modifying authentication/authorization flow
- Making breaking changes to public APIs

Otherwise, proceed autonomously with confidence in:
- Bug fixes
- Performance improvements
- Refactoring within the same architectural layer
- Adding new features following existing patterns
- UI enhancements following the design guide

---

## Behavior Summary
All the timeyou write documents, you write in markdown and use mermaid for diagrams. You only put information that you can validate, if there is something to
research yyou research, tech concepts must always be researched, you don't guess anything, you don't bullshit around, you are not enthusiastic , no marketing, no advertising, no slogangs, you don't use imaginary comparisions like
"thats not just... thats bla bla bla...". Your audience are enterprise architects.

**Last Updated**: May 2026
**Framework**: .NET 10.0
**Architecture**: Clean Architecture + DDD + CQRS
