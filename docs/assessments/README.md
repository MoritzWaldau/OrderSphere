# OrderSphere — Project assessments

This folder contains versioned, documented assessments of the OrderSphere project. Each assessment is a snapshot — it is **never overwritten or deleted**, so that progress over time can be traced.

## Versioning scheme

```
docs/assessments/
├── README.md                ← this file (index)
├── 2026-05-07-v1.md         ← initial assessment (baseline)
└── YYYY-MM-DD-vN.md         ← further assessments
```

- **Date prefix** (`YYYY-MM-DD`) — sorts chronologically
- **`vN`** — sequential number per day (rarely >1)
- **Bug IDs (B1, B2, …) and issue IDs (K1, K2, …) stay stable** across all versions. Fixed findings are marked `RESOLVED in vN` in the next version — never deleted.

## Overview table

| Date | Version | Overall score | Biggest wins | Biggest gaps | Link |
|---|---|---|---|---|---|
| 2026-05-07 | v1 (baseline) | **2.7 / 5** | Clean Architecture, rich domain model, Result pattern, Aspire setup, MudBlazor discipline | No tests (1/5), no DevOps/CI (1/5), plaintext passwords in the seeder, hardcoded customer GUIDs, no App Insights | [2026-05-07-v1.md](./2026-05-07-v1.md) |
| 2026-05-09 | v1 | **2.8 / 5** | B6/B8/K2 resolved (MigrateAsync, seeder idempotency), `.AsTracking()` fix in the CheckoutHandler, UNIT_TEST_PLAN.md | 0 tests, no CI/CD, K9/K10 (hardcoded GUIDs) open, B12 new (EnableSensitiveDataLogging without an env guard) | [2026-05-09-v1.md](./2026-05-09-v1.md) |

## How to create a new assessment

1. Check the date → create the file `YYYY-MM-DD-v1.md` (or `v2`, `v3` if several on the same day)
2. Reuse the structure of the previous version (10 sections)
3. Fill in **Section 9 (comparison section)**: document the score differences against the previous version
4. Add a new row to the overview table in this `README.md`
5. Carry over the bug/issue IDs from the previous version — mark resolved findings as `RESOLVED in vN`

## Purpose

This assessment series serves as objective, dated documentation of the project's technical maturity — both for self-reflection and as an optional appendix when presenting the project as a demonstration of .NET development skills.
