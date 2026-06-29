# 0010 — Shared infrastructure building block for Blob storage

**Status:** Accepted

## Context

Azure Blob storage was first implemented inside `Catalog.Infrastructure` (product images). The
Invoicing service then needed the same capability for invoice PDFs. Copying the blob client,
SAS-URL logic, and no-op fallback into a second service's Infrastructure would duplicate
non-trivial infrastructure code and let the two implementations drift.

The existing `BuildingBlocks` projects deliberately held only domain primitives and messaging
abstractions — no concrete infrastructure. Sharing a concrete Azure client therefore required a
decision about where it should live.

## Decision

Introduce `BuildingBlocks.Infrastructure` as a shared infrastructure layer. The blob abstraction
`IBlobStorageService` lives in `BuildingBlocks.Domain/Blob`; its Azure implementation
(`BlobStorageClients`, `AzureBlobStorageService`) and the no-op `DisabledBlobStorageService` fallback
live in `BuildingBlocks.Infrastructure`. A service's `Infrastructure` project references
`BuildingBlocks.Infrastructure` and configures the client for its own container (Catalog →
`product-images`, Invoicing → `invoices`). The blob storage implementation was relocated out of
`Catalog.Infrastructure`.

The building block holds only genuinely cross-cutting, vendor-specific infrastructure with no domain
logic. Business behaviour stays in each service.

## Consequences

- One blob implementation, shared by Catalog and Invoicing; no per-service drift.
- A service's Infrastructure may now depend on `BuildingBlocks.Infrastructure`. This does not weaken
  the no-cross-service-reference rule: the dependency is on a shared building block, not on another
  service.
- The building block carries Azure SDK dependencies (`Azure.Identity`, `Azure.Storage.Blobs`).
  Adding unrelated infrastructure here is tempting and must be resisted — only cross-cutting,
  domain-free infrastructure belongs.
- Graceful degradation is preserved centrally: when the endpoint is unconfigured,
  `DisabledBlobStorageService` is used and uploads no-op.

## Alternatives considered

- **Duplicate the blob code per service** — rejected: drift and double maintenance.
- **A standalone NuGet/storage micro-service** — rejected: over-engineered for an internal,
  vendor-specific client with no domain of its own.
