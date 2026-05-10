# OrderSphere — Projektbewertungen

Dieser Ordner enthält versionierte, dokumentierte Bewertungen des OrderSphere-Projekts. Jede Bewertung ist eine Momentaufnahme — sie wird **nie überschrieben oder gelöscht**, damit du den Fortschritt über die Zeit nachvollziehen kannst.

## Versionsschema

```
docs/assessments/
├── README.md                ← diese Datei (Index)
├── 2026-05-07-v1.md         ← Erst-Bewertung (Baseline)
└── YYYY-MM-DD-vN.md         ← weitere Bewertungen
```

- **Datums-Präfix** (`YYYY-MM-DD`) — sortiert chronologisch
- **`vN`** — laufende Nummer pro Tag (selten >1)
- **Bug-IDs (B1, B2, …) und Issue-IDs (K1, K2, …) bleiben stabil** über alle Versionen. Gefixte Findings werden in der nächsten Version als `RESOLVED in vN` markiert — nie gelöscht.

## Übersichts-Tabelle

| Datum | Version | Gesamt-Score | Größte Wins | Größte Baustellen | Link |
|---|---|---|---|---|---|
| 2026-05-07 | v1 (Baseline) | **2.7 / 5** | Clean Architecture, Rich Domain Model, Result-Pattern, Aspire-Setup, MudBlazor-Disziplin | Keine Tests (1/5), kein DevOps/CI (1/5), Klartext-Passwörter im Seeder, hardcoded Customer-GUIDs, kein App Insights | [2026-05-07-v1.md](./2026-05-07-v1.md) |
| 2026-05-09 | v1 | **2.8 / 5** | B6/B8/K2 resolved (MigrateAsync, Seeder-Idempotenz), .AsTracking()-Fix im CheckoutHandler, UNIT_TEST_PLAN.md | 0 Tests, kein CI/CD, K9/K10 (hardcoded GUIDs) offen, B12 neu (EnableSensitiveDataLogging ohne Env-Guard) | [2026-05-09-v1.md](./2026-05-09-v1.md) |

## Wie man eine neue Bewertung erstellt

1. Datum prüfen → Datei `YYYY-MM-DD-v1.md` (oder `v2`, `v3` falls am gleichen Tag mehrere) anlegen
2. Struktur der vorherigen Version übernehmen (10 Sektionen)
3. **Sektion 9 (Vergleichs-Sektion)** ausfüllen: Score-Differenzen ggü. Vorgänger-Version dokumentieren
4. Diese `README.md` um eine neue Zeile in der Übersichts-Tabelle ergänzen
5. Bug-/Issue-IDs aus der Vorgänger-Version übernehmen — gelöste Findings als `RESOLVED in vN` markieren

## Ziel

Diese Bewertungs-Reihe dient als objektive, datierte Dokumentation der technischen Reife des Projekts — sowohl für dich selbst (Selbst-Reflexion) als auch als optionaler Anhang, wenn du das Projekt als Demonstration deiner .NET-Entwicklerfähigkeiten zeigst.
