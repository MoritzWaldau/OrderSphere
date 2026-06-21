# Test-Coverage-Plan: 20% → 60%

Zielmetrik: **Line/Branch-Coverage über die gesamte Codebasis** (CI-`.runsettings`,
identisch zur SonarCloud-Zahl). Kein Include-Filter — alle Logik-Assemblies zählen.

## Ausgangslage

> Phase 0 abgeschlossen — siehe [Phase 0: Ergebnis](#phase-0--ergebnis-abgeschlossen).
> Die historisch genannten „~20%" sind veraltet/branch-bezogen. Die gemessene
> Gesamt-Baseline (gesamte Codebasis, `.runsettings`, ohne Include-Filter) ist:
>
> **42,0% Line / 32,8% Branch** (30 Assemblies, 9.268 messbare Zeilen).

Bis Phase 0 existierten zwei konkurrierende Coverage-Konfigurationen:

| Datei | Filter | gemessen | Ergebnis |
|---|---|---|---|
| `.runsettings` | kein Include (CI/Sonar-Gate) | gesamte Codebasis | **42% Line** |
| `coverage.runsettings` (entfernt) | Include nur Domain + `Catalog.Application` | 8 Assemblies | 66,1% Line |

Die Gesamtzahl ist maßgeblich. Sie wird von den 0%-Schichten gedrückt — nicht von
der Domain-Schicht, die bereits gut abgedeckt ist.

### Gemessene Baseline (Stand 2026-06-20, gesamte Codebasis)

Nach Phase-0-Excludes (`*.AppHost`, `*.ServiceDefaults`): **42,9% Line / 35,5% Branch**.
Maschinell erzeugter Report: [`docs/coverage/SummaryGithub.md`](coverage/SummaryGithub.md).

**ROI-Rangliste — die größten ungetesteten Massen** (ungedeckte Zeilen, absteigend; sie
bestimmen die Phasen-Reihenfolge):

| Assembly | Line % | ungedeckte Zeilen | Phase |
|---|---:|---:|---|
| OrderSphere.Web | 13,6% | 1.746 | 4 (Blazor, teuer) |
| OrderSphere.Catalog.Application | 45,5% | 431 | 1 |
| OrderSphere.Catalog.Infrastructure | 29,8% | 319 | 2 |
| OrderSphere.Ordering.Application | 53,4% | 282 | 1 |
| OrderSphere.Ordering.Api | 4,9% | 271 | 3 |
| OrderSphere.Advisory.Api | 39,6% | 240 | 3 |
| OrderSphere.BuildingBlocks.EventBus.AzureServiceBus | 4,4% | 213 | 2 (schwer) |
| OrderSphere.Ordering.Worker | 52,1% | 186 | 2/3 |
| OrderSphere.Ordering.Infrastructure | 61,1% | 164 | 2 |
| OrderSphere.Notification.Worker | 36,4% | 134 | 2/3 |
| OrderSphere.Payment.Worker | 32,9% | 110 | 2/3 |
| OrderSphere.Payment.Infrastructure | 32,2% | 103 | 2 |
| OrderSphere.BuildingBlocks | 62,6% | 100 | 1 |
| OrderSphere.Advisory.Infrastructure | 0% | 94 | 2 |
| OrderSphere.Webhooks.Application | 48,3% | 92 | 1 |
| OrderSphere.Payment.Application | 0% | 53 | 1 |

Bereits gut abgedeckt (kein Handlungsbedarf): alle `*.Domain` (82–100%),
`UserProfile.Application` 77%, `Basket.Application` 82%, `Mcp.Server` 76,6%.

**Arithmetik zum 60%-Ziel:** Nenner nach Excludes ≈ 8.486 messbare Zeilen, aktuell
≈ 3.639 gedeckt. 60% = 5.092 gedeckte Zeilen → **+1.453 Zeilen** nötig. `Web` (1.746
ungedeckte Zeilen) ist die größte Einzelmasse; 60% sind aber **auch ohne** vollständige
Web-Abdeckung erreichbar, wenn Application/Infrastructure/Api konsequent getestet werden
(Phase 1–3). `Web` bleibt Puffer.

### Konsequenz für die Strategie

Mehr Domain-Tests bringen nichts mehr. Der Hebel liegt im Erschließen der ungetesteten
**Application-** und **Infrastructure-Logik** — mit den Test-Patterns, die bereits im Repo
etabliert sind:

- SQLite-`DbContextFactory` (z.B. `tests/OrderSphere.Catalog.Tests/Helpers/CatalogDbContextFactory.cs`)
- `FakeHttpHandler` für HTTP-Clients (`tests/OrderSphere.Ordering.Application.Tests/Infrastructure/FakeHttpHandler.cs`)
- xUnit + FluentAssertions + NSubstitute + MockQueryable.NSubstitute

## Phase 0 — Ergebnis (abgeschlossen)

Umgesetzt auf Branch `chore/coverage-phase0`:

1. **Eine Coverage-Quelle.** `coverage.runsettings` entfernt (war nirgends in der CI
   verdrahtet, erzeugte nur die irreführenden 66%). `.runsettings` ist alleinige Quelle
   und in der Solution (`OrderSphere.slnx`) als Test-Runsettings verlinkt.
2. **Echtes Gate statt Schein-Gate.** Befund: der Collector-`<Threshold>` von 70 ließ
   `dotnet test` trotz 35,5% Branch mit Exit 0 durchlaufen — er blockt nicht. Das tote
   `<Threshold>` wurde entfernt; das enforcende Gate liegt jetzt in
   `.github/workflows/ci.yml` (Step „Coverage gate"), das die Line-Rate aus dem
   Cobertura-Report parst und unter dem Minimum hart fehlschlägt.
   Staging-Leiter: **40 → 48 → 55 → 60** (`MIN_LINE` pro Phase anheben).
3. **Ehrlicher Nenner.** `[*.AppHost]*` und `[*.ServiceDefaults]*` aus der Messung
   ausgeschlossen (Aspire-Orchestrierung bzw. OTel/Health/Logging-Wiring — keine
   unit-testbare Logik). Effekt: 42,0% → **42,9%** Line. Migrations/generierter Code
   bleiben per `ExcludeByFile`/`ExcludeByAttribute` außen vor. `Web` und `Bff` bleiben
   bewusst **in** der Messung (echte Business-/Gateway-Logik).
4. **Baseline dokumentiert.** Maschineller Report unter `docs/coverage/`, Zahlen oben
   eingearbeitet. Nach jedem Phasenabschluss neu erzeugen und `MIN_LINE` nachziehen.

Gate-Verlauf: Phase 0 `MIN_LINE=40` (Ist 42,9%) → Phase 1 `MIN_LINE=48` (Ist 50,5%) →
Phase 2 `MIN_LINE=50` (Ist 50,7%) → Phase 3 `MIN_LINE=55` (Ist 56,4%) →
Phase 4 `MIN_LINE=58` (Ist 58,8%).

## Phase 1 — Ergebnis (abgeschlossen)

21 neue Testdateien, ~107 neue Tests. **Gesamt-Line 42,9% → 50,5%** (+7,6 Punkte),
Branch 35,5% → 38,4%. Gate auf `MIN_LINE=48` angehoben.

Abgedeckte Application-Assemblies:

| Assembly | vorher | nachher |
|---|---:|---:|
| Catalog.Application | 45,5% | 84,5% |
| Payment.Application | 0% | 79,2% |
| Webhooks.Application | 48,3% | 88,8% |
| Ordering.Application | 53,4% | (Coupon-Admin + Order-Admin ergänzt) |

Umgesetzt:

- **Catalog.Application**: alle zuvor ungetesteten Handler (`GetBrands`, `DeleteBrand`,
  `UpdateBrand`, `GetAllBrandsAdmin`, `GetCategories`, `GetAllCategoriesAdmin`,
  `GetProductBySlug` (HybridCache), `GetAllProductsAdmin`, `GetAllReviews`,
  `UploadProductImage`, `ReindexSearch`) plus **alle 11 FluentValidation-Validatoren**.
- **Payment.Application**: `GetPaymentById` / `GetPaymentByOrderId` (Assembly war 0%).
- **Webhooks.Application**: `GetSubscription`, `UpdateSubscription`, `GetDeliveries`.
- **Ordering.Application**: Coupon-Admin (`CreateCoupon`, `UpdateCoupon`, `DeactivateCoupon`,
  `GetAllCoupons`) und Order-Admin (`GetOrderByIdAdmin`, `GetOrderStats`).

Offene Phase-1-Restposten (bei Bedarf, geringe Masse): `Advisory.Application` (1 Handler),
Branch-Lücken in `UserProfile.Application` / `Basket.Application`.

## Phase 2 — Ergebnis (abgeschlossen)

7 neue Testdateien, 35 neue Tests. **Gesamt-Line 50,5% → 50,7%**, Branch 38,4% → **40,0%**.
Gate auf `MIN_LINE=50` angehoben. Die Gesamt-Line wirkt flach, weil der messbare Nenner um
~600 Zeilen gewachsen ist (Brand- + AI-Search-Features zwischenzeitlich gemergt): 4.605 von
9.080 Zeilen gedeckt (Phase 1: ~4.290 von ~8.486). Per-Assembly sind die Infrastructure-
Schichten deutlich gestiegen:

| Assembly | vorher | nachher |
|---|---:|---:|
| Payment.Infrastructure | 32,2% | 67,9% |
| Catalog.Infrastructure | 29,8% | 44,8% |
| Basket.Infrastructure | 76,8% | 85,1% |
| Ordering.Infrastructure | 61,1% | 66,7% |

Umgesetzt:

- **Payment.Infrastructure**: alle drei simulierten Provider (`CreditCard`, `Invoice`, `PayPal`)
  — Authorize/Capture/Refund und Transaktions-ID-Präfix — plus `PaymentProviderFactory`
  (Case-insensitive-Auflösung, Unbekannt → null). `InternalsVisibleTo` für die Test-Assembly
  ergänzt (konsistent zum bestehenden Repo-Muster).
- **Catalog.Infrastructure**: `DisabledBlobStorageService` und `DisabledProductSearchIndex`
  (No-Op-Pfade), sowie die Konstruktor-/Normalisierungslogik von `BlobStorageClients` und
  `ProductSearchClients` (Disabled-Pfad, Endpoint-Normalisierung der Aspire-Connection-String-
  Form, Default-/Custom-Namen, `EnsureIndexAsync`-Early-Return).
- **HTTP-Client-Resilienz**: Transport-Fehler-(catch-)Pfade der bestehenden Clients ergänzt —
  Ordering `HttpCatalogClient`/`HttpBasketClient` und Basket `HttpCatalogClient` (fail-closed
  vs. graceful-degradation zu leerem Ergebnis). Die Happy-/Non-2xx-Pfade waren bereits im
  Baseline committet; Catalog `HttpOrderingClient` war schon zu 100% gedeckt.

Bewusst ausgelassen (geringe Masse bzw. hoher Test-Aufwand/Flakiness): Azure-SDK-Wrapper
(`AzureBlobStorageService`, `AzureAiProductSearchIndex` — benötigen ein Live-Konto),
`BackgroundService`-Loops (`ConversationCleanupService`, Outbox-Dispatcher) und
`RealServiceBusPublisher`-gebundene Outbox-Handler. Die Outbox-`IOutboxEventHandler`
(z.B. `OrderPlacedEventHandler`) bleiben als kleiner Restposten offen.

## Phase 3 — Ergebnis (abgeschlossen)

37 neue API-Integrationstests (`tests/OrderSphere.IntegrationTests/Api/`), die sechs Request-
Services über `WebApplicationFactory` in-process booten. **Gesamt-Line 50,7% → 56,4%** — die
55%-Stufe ist erreicht; Gate auf `MIN_LINE=55` angehoben. Branch fiel 40,0% → ~37%, weil das
Booten der vollständigen Hosts die verzweigte Startup-/Konfigurations-Verdrahtung (Program,
Swagger/RateLimiting/Versioning, Auth-Events) in den Nenner zieht; das Gate ist line-rate, daher
ohne Enforcement-Effekt.

Per-Assembly (`*.Api`):

| Assembly | vorher | nachher |
|---|---:|---:|
| Ordering.Api | 4,7% | 71,0% |
| Basket.Api | — (nicht im Nenner) | 96,6% |
| Payment.Api | — | 96,1% |
| Webhooks.Api | — | 72,6% |
| UserProfile.Api | — | 64,1% |
| Catalog.Api | — | 32,4% |

Test-Harness (`tests/OrderSphere.IntegrationTests/Api/`):

- **`TestAuthHandler`** — header-gesteuertes Authentication-Scheme (`X-Test-Sub` → `sub`-Claim,
  `X-Test-Roles` → Rollen für `IsInRole`/`ICurrentUser.Roles`). Ersetzt das produktive JWT-Bearer
  pro Test-Host; anonyme Requests challengen weiterhin mit 401.
- **`ApiTestHostExtensions`** — gemeinsame Verdrahtung: `UseSetting`-Konfiguration (OIDC- und
  Connection-String-Schlüssel werden synchron im Builder gelesen, vor `app.Build()`),
  DbContext-Tausch auf EF-InMemory bzw. in-memory SQLite (relational, für aggregierte
  Owned-Types wie `Money`), Test-Auth, Entfernen aller Hosted-Services.
- **`SqliteSchemaInitializer<T>`** — erzeugt das Schema beim Host-Start (`EnsureCreated`).
- **`ApiMarker`** je `*.Api`-Projekt — eindeutiger `WebApplicationFactory<T>`-Einstiegspunkt
  (das generierte `Program` ist intern und über die referenzierten Api-Assemblies mehrdeutig).
- Produktiv minimal: pro `Program.cs` ein `if (!IsEnvironment("Testing"))`-Guard um das
  relationale `Database.Migrate()` (würde am InMemory-/SQLite-Provider werfen).

Bewusst ausgelassen: **Advisory.Api** (Foundry-/AI-Streaming-gebunden, bereits ~35%) sowie pro
Service die externen-Client-orchestrierenden Flows (Ordering-Checkout — durch die bestehenden
Flow-Tests gedeckt) und die meisten Catalog-Admin-Write-Endpoints (Entity-Seeding-Aufwand).

## Phase 4 — Catalog-Admin + Ordering-Checkout (abgeschlossen)

13 weitere API-Integrationstests auf dem bestehenden Harness. **Gesamt-Line 56,4% → 58,8%**,
Branch 36,6% → 38,1%. Gate auf `MIN_LINE=58` angehoben.

| Assembly | nach Phase 3 | nach Phase 4 |
|---|---:|---:|
| Catalog.Api | 32,4% | 52,5% |
| Ordering.Api | 71,0% | 74,4% |

Abgedeckt:

- **Catalog-Admin-CRUD** (Brand/Category/Product create→update→delete, `GetById`), Reindex-
  Search-Disabled-Pfad (`SearchUnavailable` → 400), **interne Service-zu-Service-Endpoints**
  (Produkt-Lesen/Stock-Decrement/Restore, Reservierungs-Saga reserve→confirm→release inkl.
  Insufficient-Stock-Conflict), **öffentliche Produkt-Pfade** (Slug, Batch, Stock) und der
  authentifizierte **Review-Flow** (`IOrderingClient` als Kauf-Verifikation gestubbt).
- **Ordering-Checkout** (`POST /checkout` Happy-Path mit gestubbten Catalog-/Basket-Clients und
  Service-Bus-Publisher → 200 mit `CorrelationId`; 401 anonym) und der interne Kauf-Check.

## Phase 5 — Rest auf 60% (offen, höherer Aufwand)

Die verbleibenden ~1,2 Punkte liegen außerhalb der Minimal-API-Oberflächen:

- **Web (Blazor)**: größter verbleibender Anker (15,3%, ~2.000 coverable Lines). bUnit-
  Komponententests — hoher Aufwand pro Prozentpunkt, in der ursprünglichen Planung bewusst
  ausgeklammert.
- **Worker/Processors**: `Notification.Worker` (34,6%), `Payment.Worker` (31,6%),
  `Ordering.Worker` (51%) — testbar nach dem Muster der bestehenden Flow-Tests.
- **Bff / Gateways**: Endpoint-/YARP-Konfiguration via `WebApplicationFactory`.
- Nicht sinnvoll per Unit-Test: `Catalog.Api` gRPC-Service (benötigt gRPC-Client),
  `Advisory.Api` (Foundry-/Streaming-gebunden).

## Erwartung

| Nach Phase | Erwartung | Ist |
|---|---|---:|
| 1 | ~35–40% | 50,5% |
| 2 | ~50–55% | 50,7% |
| 3 | ~60%+ | 56,4% |
| 4 | Puffer | 58,8% |
| 5 | 60% | offen |

Phase 1 + 2 trugen die Logik-Schichten (Application/Domain/Infrastructure). Phase 3 + 4 deckten
die `*.Api`-Oberflächen breit ab (`WebApplicationFactory`), blieben aber unter den optimistischen
Schätzungen, weil der messbare Nenner durch das Booten der vollständigen Hosts (Program/Config/
Auth-Wiring) gewachsen ist. Die letzten ~1,2 Punkte auf 60% erfordern Web/Worker (Phase 5).

## Reihenfolge der Umsetzung

Phasen werden einzeln freigegeben. Pro Phase: Tests schreiben → lokal Coverage-Report →
Threshold-Stufe in `.runsettings` anheben → Review vor Commit.
