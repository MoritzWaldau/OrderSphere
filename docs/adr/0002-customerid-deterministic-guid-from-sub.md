# 0002 — `CustomerId` as a deterministic v5 GUID derived from `sub`

**Status:** Accepted

## Context

The Auth0 `sub` claim (format `auth0|<opaque_id>`) is the single source of caller identity. Order,
basket, and webhook-subscription aggregates need a strongly-typed, fixed-width key. Storing the raw
opaque `sub` string in every aggregate is wide, leaks the identity-provider format into domain keys,
and complicates indexing. There is no shared user table across services to allocate a surrogate key.

## Decision

Derive `CustomerId` (a GUID) deterministically from `sub` via `CustomerId.FromSub(sub)` — an RFC 4122
v5 GUID (SHA-256 of `sub`). Aggregates in Ordering, Basket, and Webhooks key on `CustomerId`.
UserProfile separately keeps the raw `sub` (`CustomerProfile.Subject`, unique index) as its lookup
key. Both representations originate from the same `sub`; they are never joined directly across service
boundaries — consistency rests solely on `FromSub` being deterministic.

See [../auth/role-model.md](../auth/role-model.md#identity-derivation-sub) for the full mapping.

## Consequences

- A given user maps to exactly one `CustomerId` GUID and one `Subject` string, without a shared key
  registry or cross-service foreign key.
- Aggregates use a compact, fixed-width, strongly-typed key and never store the Auth0 identifier.
- `FromSub` must remain stable forever: changing the hash or algorithm re-keys every existing
  aggregate. It is effectively a frozen contract.
- Reversing `CustomerId` back to `sub` is not possible; flows that need the raw `sub` (e.g. profile
  lookup) must carry it from the token, not reconstruct it.

## Alternatives considered

- **Store raw `sub` everywhere** — rejected: wide keys, provider format in domain.
- **Central user-ID service issuing surrogate keys** — rejected: adds a synchronous dependency and a
  shared store, against database-per-service ([0004](0004-database-per-service.md)).
