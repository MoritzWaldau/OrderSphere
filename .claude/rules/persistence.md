---
paths:
  - "src/**/Infrastructure/**/*.cs"
  - "src/**/*DbContext*.cs"
  - "src/**/EntityConfigurations/**"
  - "src/**/Migrations/**"
---

EF Core conventions:
- Every `AuditableEntity` subclass gets `builder.HasQueryFilter(x => !x.IsDeleted)` in its EF configuration. The filter is global — do not repeat `!x.IsDeleted` in query handlers.
- Use `IgnoreQueryFilters()` only when deleted rows must be read explicitly (e.g., admin restore flows).
- New entity → new file in `<Service>.Infrastructure/EntityConfigurations/`.
- Full EF migration commands per service are in [docs/architecture.md](../../docs/architecture.md).
