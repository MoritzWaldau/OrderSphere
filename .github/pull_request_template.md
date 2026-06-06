<!--
PR-Titel muss Conventional Commits folgen (wird per pr-title-lint geprüft):
  feat | fix | refactor | docs | style | test | chore | ci | build | perf | revert
Beispiel: feat(catalog): add product slug uniqueness check
-->

## Zusammenfassung

<!-- Was ändert dieser PR und warum? Eine knappe, fachliche Beschreibung. -->

## Art der Änderung

- [ ] feat — neues Feature
- [ ] fix — Fehlerbehebung
- [ ] refactor — Umbau ohne Verhaltensänderung
- [ ] docs / style / test / chore / ci / build / perf

## Checkliste

- [ ] Build grün (`dotnet build OrderSphere.slnx`)
- [ ] Tests grün und ggf. ergänzt (`dotnet test`)
- [ ] Keine neuen Compiler-Warnungen (TreatWarningsAsErrors aktiv)
- [ ] EF-Migrationen enthalten, falls Schemaänderung (rückwärtskompatibel oder abgestimmt)
- [ ] Architektur-Konventionen eingehalten (Result<T>, Layer-Abhängigkeiten, keine Cross-Service-Projektrefs)
- [ ] Doku aktualisiert (docs/, README), falls relevant
- [ ] Keine Secrets/Connection-Strings committet

## Breaking Changes

<!-- Öffentliche Verträge zur UI/zu anderen Diensten betroffen? Wenn ja: welche und wie migrieren. -->

## Verweise

<!-- Closes #<issue>, verwandte PRs, Kontext -->
