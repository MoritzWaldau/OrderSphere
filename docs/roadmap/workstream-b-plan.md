# Workstream B — Domänen- & Business-Capabilities — Umsetzungsplan

Epic [#60](https://github.com/MoritzWaldau/OrderSphere/issues/60). Funktionsbreite des
E-Commerce-Spektrums: Payment, Returns, Coupons, Multi-Currency, Multi-Tenancy, B2B,
Notifications, Dokumente.

## Kontext

Das Epic erweitert OrderSphere um acht produktions- bzw. showcase-relevante Geschäftsfähigkeiten.
Die bestehende Architektur (Clean Architecture + CQRS/MediatR + DDD, `Result<T>`,
`AuditableEntity` + Soft-Delete-Query-Filter, Outbox/Inbox-Eventing über Azure Service Bus,
.NET Aspire AppHost) ist Grundlage; jedes Item folgt den etablierten Mustern.

## Entscheidungen (mit dem Auftraggeber abgestimmt)

1. **Sequenzierung:** Phasen-Label-Reihenfolge. Phase 3 zuerst (B1, B2, B3, B7, B8), danach
   Phase 4 (B4, B5, B6). **Ein Branch + ein PR pro Sub-Issue** (`feat/task_b*`), wie im
   bisherigen Workflow.
2. **B5 Multi-Tenancy:** **Volle org-level Mandantenfähigkeit** (Tenant-Onboarding,
   per-Tenant-Konfiguration, Tenant-Verwaltung). Erfordert eine **neue ADR, die ADR 0009 ablöst**.
3. **B8 Invoice/PDF:** **Neuer dedizierter Invoicing/Document-Service** mit eigener Persistenz und
   Download-Endpoint. Blob-Abstraktion wird aus Catalog nach BuildingBlocks gehoben.

## Reihenfolge & Status

| # | Issue | Item | Phase | Branch | Status |
|---|-------|------|-------|--------|--------|
| 1 | [#70](https://github.com/MoritzWaldau/OrderSphere/issues/70) | B1 Stripe-Payment-Provider | 3 | `feat/task_b1` | ✅ umgesetzt (Review offen) |
| 2 | [#71](https://github.com/MoritzWaldau/OrderSphere/issues/71) | B2 Returns/RMA-Workflow | 3 | `feat/task_b2` | ✅ umgesetzt (Review offen) |
| 3 | [#72](https://github.com/MoritzWaldau/OrderSphere/issues/72) | B3 Coupon-Engine (Staffel/Kategorie) | 3 | `feat/task_b3` | offen |
| 4 | [#76](https://github.com/MoritzWaldau/OrderSphere/issues/76) | B7 Multi-Channel-Notifications | 3 | `feat/task_b7` | offen |
| 5 | [#77](https://github.com/MoritzWaldau/OrderSphere/issues/77) | B8 Invoice-/Dokument-Service | 3 | `feat/task_b8` | offen |
| 6 | [#73](https://github.com/MoritzWaldau/OrderSphere/issues/73) | B4 Multi-Currency / Money | 4 | `feat/task_b4` | offen |
| 7 | [#74](https://github.com/MoritzWaldau/OrderSphere/issues/74) | B5 Multi-Tenancy (org-level) | 4 | `feat/task_b5` | offen |
| 8 | [#75](https://github.com/MoritzWaldau/OrderSphere/issues/75) | B6 B2B-Partner-API + Quotas | 4 | `feat/task_b6` | offen |

---

## B1 — Echter Payment-Provider (Stripe) hinter `IPaymentProvider`

**Ziel:** Stripe (Test-Modus) als echte Provider-Implementierung hinter der bestehenden
`IPaymentProvider`-Abstraktion; inbound Webhook für asynchrone Capture-Bestätigung.

**Bestand:** `IPaymentProvider` (Authorize/Capture/Refund) in
`Payment.Infrastructure/Providers/`, simulierte Provider (CreditCard/PayPal/Invoice),
`PaymentProviderFactory`, `PaymentProcessor`-Worker (Authorize→Capture in einem Schritt),
`OrderConfirmationFailedProcessor` (Refund-Pfad). Outbox/Inbox etabliert.

**Umsetzung:**
- NuGet `Stripe.net` (Ask-before: durch Issue vorgegeben) zu `Payment.Infrastructure`.
- `StripePaymentProvider : IPaymentProvider` (`MethodName = "Stripe"`) via PaymentIntents API:
  Authorize = PaymentIntent erstellen (`capture_method=manual`), Capture = PaymentIntent capturen,
  Refund = Refund erstellen. Fehler → `Result.Failure`, nie Exceptions für Geschäftsfehler.
- `StripeOptions` (ApiKey, WebhookSecret) + Registrierung in `Payment.Infrastructure/DependencyInjection`.
- Inbound Webhook-Endpoint in `Payment.Api` (`/api/v1/payments/webhooks/stripe`, anonymous,
  Signaturprüfung via `WebhookSecret`): verarbeitet `payment_intent.succeeded` /
  `payment_intent.payment_failed` → aktualisiert `PaymentRecord`, schreibt Outbox-Event
  (`PaymentProcessedIntegrationEvent`). Idempotenz über Inbox bzw. Stripe-Event-Id.
- AppHost: `stripe-api-key` / `stripe-webhook-secret` als `AddParameter(secret: true)` →
  `WithEnvironment` an Payment.Api und Payment.Worker.
- Default-Provider konfigurierbar (Stripe nur wenn Key gesetzt, sonst simuliert) — lokaler
  Dev-Fallback bleibt erhalten.

**Verifikation:** Unit-Tests für `StripePaymentProvider` (mit Stripe-Test-Keys / gemocktem
HttpClient), Webhook-Signaturprüfung, End-to-End-Checkout im Aspire-Run gegen Stripe-Test-Modus.

---

## B2 — Returns / RMA-Workflow

**Ziel:** Rückgabe-/Erstattungs-Prozess als Use-Cases in Ordering + Payment.
Zustandsmaschine `Requested → Approved → Refunded / Rejected`.

**Bestand:** `Order` ist event-sourced (`Order.cs`, `OrderEvents.cs`, `OrderStatus`).
Refund existiert in Payment (`OrderConfirmationFailedProcessor` → `provider.RefundAsync`,
`PaymentRefundedIntegrationEvent`).

**Umsetzung:**
- Neue Aggregat-/Entity `ReturnRequest` (Ordering.Domain) mit Status-Enum
  `Requested/Approved/Rejected/Refunded` und Übergangs-Guards; pro Order-Item-Auswahl + Menge + Grund.
- Use-Cases (Ordering.Application/Features/Returns): `RequestReturn` (Kunde),
  `ApproveReturn`/`RejectReturn` (Staff). Validatoren + DTOs + Endpoints (Owner/Staff-Policy).
- Approve → Outbox `RefundRequestedIntegrationEvent` (neuer Contract in BuildingBlocks.Contracts).
- Payment.Worker: `RefundRequestedProcessor` ruft `provider.RefundAsync`, markiert
  `PaymentRecord.MarkRefunded()`, publiziert `PaymentRefundedIntegrationEvent`.
- Ordering.Worker konsumiert `PaymentRefundedIntegrationEvent` → `ReturnRequest` auf `Refunded`,
  Kunde wird benachrichtigt (Outbox → Notification).

**Verifikation:** Domain-Tests Zustandsmaschine, Handler-Tests, Integrationstest Return→Refund-Pfad.

---

## B3 — Coupon-Engine: Staffel-Rabatte + Kategorie-Scoping

**Ziel:** Gestaffelte Rabatte (Schwellen) und Kategorie-Scoping zusätzlich zur Flat/Percentage-Engine.

**Bestand:** `Coupon` (`Coupon.cs`) mit `DiscountType {Flat, Percentage}`, `CalculateDiscount`,
`Redeem`. Anwendung im `OrderProcessor`-Worker (Items tragen Name/Menge/Preis).

**Umsetzung:**
- `DiscountType.Tiered` + `CouponTier`-Wertobjekt (MinSubtotal → DiscountValue) als gestaffelte
  Schwellen; `CalculateDiscount` wählt höchste erfüllte Stufe.
- Kategorie-Scoping: `Coupon.ScopedCategoryIds` (optional). Rabatt nur auf Items qualifizierter
  Kategorien → Subtotal-Berechnung pro Scope. Erfordert `CategoryId` in den Order-Items/Checkout-
  Daten (Catalog liefert bereits Kategorie; Datenfluss erweitern wenn nötig).
- Admin-Endpoints/DTOs für Tiers + Kategorien erweitern; EF-Konfiguration (owned collection / JSON).

**Verifikation:** Domain-Tests (Staffel-Grenzen, Kategorie-Filter), Handler-/Validator-Tests.

---

## B7 — Multi-Channel-Notifications (E-Mail + SMS/Push) + Präferenzen

**Ziel:** Kanal-Abstraktion (Strategy) + Nutzer-Präferenzen (Opt-in/Opt-out, DSGVO).

**Bestand:** Notification.Worker konsumiert `OrderPlacedIntegrationEvent`, sendet nur E-Mail
(`INotificationEmailService`, Azure Communication Services bzw. Logging-Fallback).
Präferenzen gehören fachlich zu UserProfile (`CustomerProfile`).

**Umsetzung:**
- `INotificationChannel` (Strategy): `EmailChannel` (bestehend gekapselt), `SmsChannel`
  (z.B. ACS SMS / Twilio-Abstraktion), optional `PushChannel`. Auswahl per Kanal-Typ.
- Notification-Versand iteriert über aktivierte Kanäle gemäß Präferenz.
- UserProfile: `NotificationPreferences` (pro Kanal + pro Kategorie Opt-in/Opt-out, Einwilligungs-
  Zeitstempel). Use-Cases zum Lesen/Setzen + Endpoint; vom Notification-Worker via Client/Query
  abgefragt (oder als Event repliziert).
- DSGVO: Default Opt-out für nicht-transaktionale Kanäle; transaktionale Mails (Bestellung) bleiben.

**Verifikation:** Channel-Strategy-Tests, Präferenz-Handler-Tests, Worker-Integrationstest.

---

## B8 — Invoice-/Dokument-Service (PDF)

**Ziel:** Rechnungs-PDF bei Order-Bestätigung, persistiert + per Download abrufbar; Anhang im
Notification-Worker.

**Bestand:** Kein PDF-Lib. Blob-Storage existiert in Catalog (`IBlobStorageService`,
`AzureBlobStorageService`) — Catalog-lokal. Order-Bestätigung publiziert
`OrderPlacedIntegrationEvent`.

**Umsetzung:**
- Blob-Abstraktion `IBlobStorageService` + Azure-Implementierung nach
  `BuildingBlocks` heben (wiederverwendbar), Catalog auf shared Variante umstellen.
- **Neuer Service `Invoicing`** (Api/Application/Domain/Infrastructure + Worker oder Consumer):
  `Invoice`-Aggregat (Nummernkreis, Order-Referenz, Beträge, Blob-Pfad). Konsumiert
  `OrderPlacedIntegrationEvent` → erzeugt PDF (QuestPDF) → Blob-Container `invoices` → persistiert
  Invoice-Metadaten → Outbox `InvoiceGeneratedIntegrationEvent` (Blob-/SAS-URL).
- Download-Endpoint (Owner/Staff-Policy) liefert SAS-URL/Stream.
- Notification-Worker hängt PDF (per SAS-URL) an die Bestätigungs-Mail; AppHost-Wiring (DB,
  Blob-Container, Service-Registrierung, Service-Bus-Queue).

**Verifikation:** PDF-Snapshot-Test, Invoice-Domain-Tests, End-to-End Order→Invoice→Download im Aspire-Run.

---

## B4 — Multi-Currency / Money konsequent

**Ziel:** `Money(amount, currency)` durchgängig statt rohem `decimal`; Wechselkurs-Lookup,
locale-abhängige Anzeige.

**Bestand:** `Money`-Wertobjekt existiert (mit `ConvertTo(target, rate)`), genutzt für
`Product.Price`, `OrderItem.Price`, `PaymentRecord.Amount`. Noch roh `decimal`:
`Coupon.Value`, `Order.DiscountAmount/ShippingCost`, Integration-Event-Beträge.

**Umsetzung:**
- Verbleibende `decimal`-Geldfelder auf `Money` umstellen (rückwärtskompatible Schema-Prüfung —
  Ask-before bestätigt: per Migration mit Currency-Spalte + Default "EUR").
- `IExchangeRateProvider` (Lookup + Caching; konkrete Quelle z.B. statisch/extern) in BuildingBlocks
  oder geteiltem Infrastructure; `Money.ConvertTo` nutzt gelieferte Rate.
- Anzeige: locale-abhängige Formatierung im BFF/Client (CultureInfo + Währung).
- Currency-Auswahl im Checkout-Pfad durchreichen (Default EUR).

**Verifikation:** Money-/Conversion-Tests, Migrations-SQL-Review (rückwärtskompatibel), UI-Anzeige-Check.

---

## B5 — Multi-Tenancy (org-level, volle Mandantenfähigkeit)

**Ziel:** Echte Org-Mandantenfähigkeit: `TenantId`-Scoping, Tenant-Resolution aus Auth0-Claim,
Tenant-Onboarding/-Verwaltung, per-Tenant-Konfiguration.

**Bestand:** ADR 0009 (single-tenant + per-Customer-Scoping) — **wird abgelöst**. Soft-Delete-
Query-Filter ist Vorlage. `ICurrentUser` liest Auth0-Claims; `CustomerId` deterministisch aus `sub`.

**Umsetzung:**
- **Neue ADR** „Org-level Multi-Tenancy" die ADR 0009 ablöst (Vorab-Design, Ask-before).
- `TenantId` in `AuditableEntity`; globaler Query-Filter analog Soft-Delete in allen
  EntityConfigurations (`x => x.TenantId == currentTenant && !x.IsDeleted`).
- `ICurrentUser.TenantId` aus Auth0 `org_id`/Custom-Claim; Tenant-Resolution-Middleware;
  Auto-Set von `TenantId` beim Speichern (SaveChanges-Interceptor).
- Tenant-Verwaltung: Onboarding (neuer Tenant), per-Tenant-Konfiguration; Worker/Background-Kontexte
  müssen Tenant explizit setzen (kein HttpContext).
- Migrationen je Service (TenantId-Spalte + Index), Daten-Backfill-Strategie (Default-Tenant).

**Verifikation:** Filter-Isolations-Tests (Tenant A sieht B nicht), Worker-Tenant-Kontext-Tests,
Migrations-Review je Service.

---

## B6 — B2B-Partner-API mit API-Keys + Quotas

**Ziel:** Externe Partner via API-Key (zusätzlich zu Auth0-JWT) mit Quotas am ApiGateway.

**Bestand:** `OrderSphere.ApiGateway` (YARP) mit Rate-Limiting (per-User/-IP). Kein API-Key-Auth.

**Umsetzung:**
- API-Key-`AuthenticationHandler` (Header `X-API-Key`, gehashte Keys, Partner-Zuordnung) neben
  JWT-Schema; Partner-Store (Persistenz für Keys + Quota-Tarif).
- Partner-Quota: benannter `PartitionedRateLimiter` keyed nach API-Key (Tarif-abhängiges Limit).
- `"partner"`-Authorization-Policy; ausgewählte Routen für Partner freigeben.
- Verwaltung: Key-Ausgabe/-Rotation/-Widerruf (Admin-Endpoint).

**Verifikation:** Auth-Handler-Tests, Quota-/Rate-Limit-Tests, End-to-End Partner-Call.

---

## Querschnittliche Hinweise

- Jedes Item: eigener Branch von `master`, kein Commit vor Review (Working-Style KISS/SOLID).
- Neue NuGet-Pakete (Stripe.net, QuestPDF, ggf. SMS-SDK) sind durch die Issues vorgegeben, werden
  aber explizit benannt.
- Schema-Änderungen (B4, B5) per rückwärtskompatibler Migration; SQL vor Apply prüfen.
- Build: `dotnet build OrderSphere.slnx`; Tests: `dotnet test`; Lauf: Aspire AppHost.
