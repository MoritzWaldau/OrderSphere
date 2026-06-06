<!--
PR title must follow Conventional Commits (enforced by pr-title-lint):
  feat | fix | refactor | docs | style | test | chore | ci | build | perf | revert
Example: feat(catalog): add product slug uniqueness check
-->

## Summary

<!-- What does this PR change and why? A concise, technical description. -->

## Type of change

- [ ] feat — new feature
- [ ] fix — bug fix
- [ ] refactor — change without behavioural impact
- [ ] docs / style / test / chore / ci / build / perf

## Checklist

- [ ] Build green (`dotnet build OrderSphere.slnx`)
- [ ] Tests green and extended where applicable (`dotnet test`)
- [ ] No new compiler warnings (TreatWarningsAsErrors enabled)
- [ ] EF migrations included for schema changes (backward-compatible or agreed)
- [ ] Architecture conventions upheld (Result<T>, layer dependencies, no cross-service project refs)
- [ ] Documentation updated (docs/, README) where relevant
- [ ] No secrets/connection strings committed

## Breaking changes

<!-- Are public contracts to the UI or other services affected? If so: which, and how to migrate. -->

## References

<!-- Closes #<issue>, related PRs, context -->
