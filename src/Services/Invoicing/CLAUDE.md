# Invoicing Service

Generates an invoice PDF for each placed order, stores it in Blob storage, and notifies the customer.

## Project layout

Standard 4-project Clean Architecture: `Invoicing.Domain`, `Invoicing.Application`,
`Invoicing.Infrastructure`, `Invoicing.Api`. Layer dependencies point inward as usual. PDF rendering
(`QuestPdfInvoiceService`) and Blob storage live in `Infrastructure`; the shared blob client comes
from `BuildingBlocks.Infrastructure` via `IBlobStorageService`.

## Service Bus consumer runs inside the Api — no Worker project

Unlike Ordering and Payment, the queue consumer (`Api/Workers/InvoiceProcessor.cs`) is a
`BackgroundService` hosted **inside `Invoicing.Api`**, not a separate `.Worker` project. The Api also
serves the JWT-protected download endpoints, so one process owns both the event handling and the read
surface. **Do not split out a Worker project** without an architecture review.

Flow: `invoice-generation` (carrying `OrderPlacedIntegrationEvent`) →
`GenerateInvoiceCommand` (render PDF, persist `Invoice`, upload blob) → publish
`InvoiceGeneratedIntegrationEvent` to `invoice-ready` → consumed by `Notification.Worker`.

## No transactional outbox on the publish

`InvoiceProcessor` publishes `invoice-ready` **directly** through `IEventBus`, with no outbox. The
guarantee is at-least-once: inbox dedupe on the `invoice-generation` side (`IInboxStore`) prevents
reprocessing, and the downstream `InvoiceGeneratedProcessor` dedupes on the event `Id`. If the publish
fails after the invoice is persisted, the message is abandoned and redelivered; the inbox check makes
the retry idempotent. **Do not assume outbox semantics here** — this is a deliberate deviation from the
Ordering/Payment outbox pattern, justified by the single-publish, idempotent-consumer shape.

## Authorization

Download endpoints (`api/v1/invoices/by-order/{orderId}`) require authentication and enforce
ownership: a caller may read an invoice only if they are in role `admin` or the invoice's
`CustomerEmail` matches their token. Keep this owner-or-admin guard on every new invoice read endpoint.

## Migrations

```
dotnet ef migrations add <Name> \
  -p src/Services/Invoicing/OrderSphere.Invoicing.Infrastructure \
  -s src/Services/Invoicing/OrderSphere.Invoicing.Api
```
