# Service Contract Conventions

## Directory layout

```
contracts/
  proto/
    v1/        ← current stable gRPC contracts
    v2/        ← next-version contracts (when a breaking change is introduced)
  openapi/
    v1/        ← current stable REST contracts
    v2/        ← next-version REST contracts
```

## gRPC / Protobuf versioning

- Package declaration: `package ordersphere.<service>.v1;`
- Option: `option csharp_namespace = "OrderSphere.Contracts.<Service>.V1";`
- Breaking changes (field removal, type change, rename) require a new package folder (`v2/`).
- Backward-compatible additions (new fields, new RPCs) stay in the existing version.
- Old major versions are supported for one major application release cycle.

## REST / OpenAPI versioning

- URL prefix: `/api/v1/`, `/api/v2/`
- One OpenAPI spec file per service per major version: `catalog.v1.yaml`
- Use `info.version` semantic versioning within the spec.

## NuGet contracts packages

Each service publishes a contracts package consumed by other services and by the API gateway:

- `OrderSphere.Contracts.Catalog.V1`
- `OrderSphere.Contracts.Ordering.V1`
- `OrderSphere.Contracts.UserProfile.V1`

Packages contain generated gRPC client stubs and shared event DTOs. No domain logic.

## Event schema

Service Bus events use JSON. Schema stored alongside the proto definitions:

```
proto/v1/events/CheckoutCartEvent.json
```

Include `$schema` and `type` discriminator field in every event payload.
