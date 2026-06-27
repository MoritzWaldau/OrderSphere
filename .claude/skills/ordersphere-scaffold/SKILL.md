---
name: ordersphere-scaffold
description: Generates a complete OrderSphere vertical slice — Command/Query + Handler + Validator + DTO + (optionally) Entity + EF Configuration + Endpoint — following the conventions in ordersphere-patterns and CLAUDE.md. Use when adding a new use-case or aggregate to an existing service.
---

# OrderSphere scaffold

Generates a complete vertical slice for one use-case inside an existing OrderSphere service.
This skill produces files; for the conventions that govern those files, read
`.claude/skills/ordersphere-patterns/SKILL.md` first. For the system map and EF migration
commands, read `docs/architecture.md`.

**Audience:** enterprise architects. State decisions directly.

## When to use

Invoke when:
- Adding a new command or query to an existing aggregate.
- Adding a new aggregate (entity + use-cases) to an existing service.
- Regenerating a slice after a rename or structural change.

Do **not** invoke to scaffold a brand-new service (that requires the service skeleton, a new
DbContext, migrations infrastructure, and Aspire wiring — all out of scope here).

## Required inputs

Collect these before generating any file:

| Input | Example |
|---|---|
| **Service** | `Ordering` |
| **Aggregate** | `Coupon` |
| **UseCase** | `CreateCoupon` |
| **Type** | Command or Query |
| **Returns** | `Guid` (Command) / `CouponAdminDto` (Query) |
| **New entity?** | Yes / No |
| **Entity fields** | `Code: string`, `Value: decimal`, … |
| **Admin-only?** | Yes → nest under `Admin/`; No → top-level |
| **Endpoint needed?** | Yes / No (delegate to `endpoint-author` agent if yes) |

## Step 1 — Application layer

### 1a. Command or Query record

**Path:**
`src/Services/<Service>/OrderSphere.<Service>.Application/Features/<Aggregate>/[Admin/]<UseCase>/<UseCase>Command.cs`
(or `…Query.cs` for a query)

```csharp
// Command
public sealed record <UseCase>Command(<parameters>) : ICommand<Result<<ReturnType>>>;

// Query
public sealed record <UseCase>Query(<parameters>) : IQuery<Result<<ReturnType>>>;
```

Marker interfaces from `OrderSphere.BuildingBlocks.Abstraction`.

### 1b. Handler

**Path:** same folder as the record, `<UseCase>CommandHandler.cs` / `<UseCase>QueryHandler.cs`

```csharp
public sealed class <UseCase>CommandHandler(
    I<Service>DbContext context,
    ILogger<<UseCase>CommandHandler> logger
) : ICommandHandler<<UseCase>Command, Result<<ReturnType>>>
{
    public async Task<Result<<ReturnType>>> Handle(<UseCase>Command request, CancellationToken ct)
    {
        try
        {
            // business logic — return Result<<ReturnType>>.Failure(<Aggregate>Errors.XyzError) for expected failures
            // ...
            await context.SaveChangesAsync(ct);
            return Result<<ReturnType>>.Success(/* value */);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in <UseCase>");
            return Result<<ReturnType>>.Failure(<Aggregate>Errors.UnknownError);
        }
    }
}
```

Canonical template: `src/Services/Ordering/OrderSphere.Ordering.Application/Features/Coupon/Admin/CreateCoupon/CreateCouponCommandHandler.cs`

For queries, use `.AsNoTracking()` on all reads.

### 1c. Validator (commands only)

**Path:** same folder, `<UseCase>CommandValidator.cs`

```csharp
public sealed class <UseCase>CommandValidator : AbstractValidator<<UseCase>Command>
{
    public <UseCase>CommandValidator()
    {
        RuleFor(x => x.<Field>).NotEmpty();
        // ...
    }
}
```

The `ValidationBehavior` MediatR pipeline runs validators automatically — do not call them manually.

### 1d. DTO (if the handler returns a projected type)

**Path:** `src/Services/<Service>/OrderSphere.<Service>.Application/Models/<ReturnType>Dto.cs`

```csharp
public sealed record <ReturnType>Dto(<properties>);
```

All DTOs are `sealed record`. No logic, no methods.

## Step 2 — Domain layer (new entity only)

Skip this step if the aggregate already exists.

### 2a. Strongly-typed ID

**Path:** `src/BuildingBlocks/OrderSphere.BuildingBlocks.Domain/StronglyTypedIds/<Aggregate>Id.cs`
(if a shared ID is needed) or `src/Services/<Service>/OrderSphere.<Service>.Domain/ValueObjects/<Aggregate>Id.cs`

```csharp
public readonly record struct <Aggregate>Id(Guid Value)
{
    public static <Aggregate>Id New() => new(Guid.CreateVersion7());
    public static <Aggregate>Id Empty => new(Guid.Empty);
    public static <Aggregate>Id From(Guid v) => new(v);
    public override string ToString() => Value.ToString();
}
```

### 2b. Entity

**Path:** `src/Services/<Service>/OrderSphere.<Service>.Domain/Entities/<Aggregate>.cs`

```csharp
public class <Aggregate> : AuditableEntity<<Aggregate>Id>, IAggregateRoot
{
    // properties with private setters
    
    private <Aggregate>() { } // required by EF
    
    public <Aggregate>(<constructor parameters>)
    {
        Id = <Aggregate>Id.New();
        // initialise properties
    }
    
    // behaviour methods return Result / Result<T>
}
```

### 2c. Domain errors

**Path:** `src/Services/<Service>/OrderSphere.<Service>.Domain/Errors/<Aggregate>Errors.cs`

```csharp
public static class <Aggregate>Errors
{
    public static readonly Error NotFound =
        new("<Aggregate>.NotFound", "…", ErrorType.NotFound);
    public static readonly Error UnknownError =
        new("<Aggregate>.Unknown", "An unexpected error occurred.", ErrorType.Failure);
    // add conflict, validation errors as needed
}
```

## Step 3 — Infrastructure layer (new entity only)

### 3a. EF configuration

**Path:** `src/Services/<Service>/OrderSphere.<Service>.Infrastructure/EntityConfigurations/<Aggregate>Configuration.cs`

```csharp
public sealed class <Aggregate>Configuration : IEntityTypeConfiguration<<Aggregate>>
{
    public void Configure(EntityTypeBuilder<<Aggregate>> builder)
    {
        builder.ToTable("<table_name>");          // lowercase snake_case
        builder.HasKey(x => x.Id);
        builder.HasQueryFilter(x => !x.IsDeleted);   // mandatory for every AuditableEntity

        // property mappings
        builder.Property(x => x.<Field>).HasMaxLength(…).IsRequired();
        // indexes, relationships, seed data as needed
    }
}
```

**This filter is mandatory** — omitting it means deleted rows leak into all queries.

### 3b. DbSet registration

Add `DbSet<<Aggregate>> <Aggregates> { get; }` to:
1. `I<Service>DbContext` in `<Service>.Application/Abstractions/I<Service>DbContext.cs`
2. The concrete context in `<Service>.Infrastructure/Persistence/<Service>DbContext.cs`

### 3c. EF migration (delegate to `migration-author` agent)

After the EF configuration is in place, invoke the **`migration-author`** agent with the
service name and a descriptive migration name (e.g. `Add<Aggregate>`). It will run
`dotnet ef migrations add` with the correct `-p`/`-s` arguments and review the SQL.

## Step 4 — Api layer (endpoint)

Delegate to the **`endpoint-author`** agent. Provide:
- Service name
- Command/query name
- HTTP method + route pattern
- Authorization policy (public, customer-owned, admin)

Canonical reference: `src/Services/Ordering/OrderSphere.Ordering.Api/Endpoints/CouponEndpoints.cs`

## Checklist before finishing

- [ ] Command/Query record implements `ICommand<Result<T>>` / `IQuery<Result<T>>`.
- [ ] Handler catches and converts exceptions; never swallows `OperationCanceledException`.
- [ ] Query handler uses `.AsNoTracking()`.
- [ ] DTO is a `sealed record`, not a class.
- [ ] New entity inherits `AuditableEntity<TId>` and has a private EF constructor.
- [ ] EF config has `HasQueryFilter(x => !x.IsDeleted)`.
- [ ] No `!x.IsDeleted` in any handler query — the filter applies automatically.
- [ ] All I/O is `async`/`await`. No `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`.
- [ ] No new NuGet dependency introduced without asking (see `CLAUDE.md` § "Ask before").
- [ ] Non-trivial schema change confirmed with user before migration.

## Verification

```
dotnet build OrderSphere.slnx
dotnet test --filter "Name~<UseCase>"
```

The CI line-coverage gate (`MIN_LINE` in `.github/workflows/ci.yml`) must hold — new handler
logic needs accompanying tests. See `docs/test-coverage-plan.md` for the coverage strategy.
