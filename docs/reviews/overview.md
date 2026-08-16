# SteelTube — Overall Project Review

Date: 2026-08-13 · Scope: whole solution (Domain, Application, Infrastructure,
Desktop stub, tests) · Review docs: see `docs/reviews/domain.md`,
`docs/reviews/application.md`, `docs/reviews/infrastructure.md`.

## Architecture at a glance

```
SteelTube.Desktop (WPF stub — not implemented, references all three)
        │
        ▼
SteelTube.Infrastructure  (SQLite, repos, UoW, device context, composition root)
        │  references
        ▼
SteelTube.Application     (abstractions + use-case handlers)
        │  references
        ▼
SteelTube.Domain          (entities, value objects, services — no deps)
```

Dependency direction is correct and clean; `Domain` has no external
dependencies at all. The manual composition root (`CompositionRoot.cs`)
reflects a deliberate "no DI container" constraint (SAD Goal 5). .NET Framework
4.8 / WPF / SQLite, x64 target. Solution builds clean on `Debug|x64`.

## What is working well

- **Architecture discipline**: layers separated, dependencies point inward,
  DTOs isolate the domain, repositories behind interfaces.
- **Event-sourced-style inventory**: append-only `InventoryOperation` ledger is
  the source of truth; `InventoryBalance` is a rebuildable projection (SAD
  22), with the sign convention centralized in `SignedLengthMeters`.
- **Domain modeling**: value objects with construction-time invariants;
  material identity = Diameter + Thickness; length/weight convertible through a
  snapshotting catalogue (`WeightPerMeterUsed`).
- **Persistence care**: decimal-as-text for exactness (SAD 60), WAL, foreign
  keys on, parameterized SQL, indexed history columns (SAD 59).
- **Traceability**: nearly every type/method carries a SAD/SRS reference — rare
  and valuable.
- **End-to-end acceptance tests exist** for the main inventory flow.

## Verified build & test status

- Build: **green** (one MSTest analyzer warning, `MSTEST0044`, on
  `LengthTests`).
- Test run: **7 passed / 5 failed / 12 total**.
  - `LengthTests.FromMeters_rejects_non_positive_values` ×2 fail: MSTest 4.x
    `DataRow` passes `int` literals to a `decimal` parameter — type mismatch
    (domain).
  - All 3 `CompositionRootTests` fail at `Setup` with
    `FileNotFoundException: SQLitePCLRaw.core, Version=2.1.12.3116` — dependency
    version mismatch (infrastructure, issue I1).

So the "suite is green" assumption does not currently hold; the two failures
above should be fixed before adding more tests.

## Cross-cutting issues (all layers)

1. **Error contract is not honored end-to-end.** Domain declares
   `DomainException`/`DomainValidationException`/`BusinessRuleViolationException`
   but value objects throw BCL exceptions; Application defines only
   `UseCaseValidationException` and lets everything else escape. The Desktop
   will therefore receive an unpredictable mix of exception types. This is the
   single most important cross-cutting fix before UI work.
2. **Unused/planned surface creates confusion.** Repos implement
   `GetHistoryAsync`, `GetByOriginDeviceAfterSequenceAsync`,
   `GetAllForTubeSpecificationAsync`, `GetByIdAsync`, `RebuildAllAsync` with no
   consumers; `Newtonsoft.Json` is referenced by Application + Infrastructure
   but never used; domain factories for adjustment operations have no handlers.
   This is roadmap scaffolding (sync SAD 29-33, history SRS 10/12), but it
   should be labeled as such — or trimmed.
3. **Dependency management is inconsistent.**
   - `packages.config` + hand-written `app.config` binding redirects, no
     `nuget.config`; `packages/` is git-ignored so a fresh clone depends on
     restore, and the redirects are the only thing holding the SQLitePCLRaw
     mismatch together (and not even that in the test host).
   - Mixed native providers (`SQLite` 3.53.4 + `SQLitePCLRaw.lib.e_sqlite3`).
   - `LangVersion` differs per config in every csproj: `Debug|x64` = 7.3,
     `Release|x64` = latest, while `Directory.Build.props` sets `latest` — the
     Debug build does not exercise the same language features as Release.
4. **Concurrency model is implicit.** One shared `SqliteConnection` plus a
   mutable ambient `CurrentTransaction`; nothing enforces single-threaded
   access. Fine today, a foot-gun once the Desktop runs async.
5. **Testing gaps.** Application layer has zero tests; Infrastructure tests are
   currently broken and don't cover catalogue/partners/rebuild; Domain misses
   most value-object entities.

## Feature-completeness vs. the modeled domain

| Modeled in Domain/Infrastructure        | Wired to a use case? |
|-----------------------------------------|----------------------|
| Purchase (AddStock)                     | Yes                  |
| Sale (RemoveStock)                      | Yes                  |
| AdjustmentIncrease / AdjustmentDecrease | **No** (factories exist) |
| Operation history query                 | **No** (`GetHistoryAsync` unused) |
| Current stock projection                | Yes                  |
| Weight catalogue add/update/list        | Yes                  |
| Business partners create/list           | Yes; edit (Rename/SetRoles) **not** wired |
| Device identity + local sequence        | Yes (used by stock commands) |
| Sync import/export / merge              | **No** (only scaffolding: `OriginDeviceId`, `OriginSequenceNumber`, export query) |
| Schema migration                        | **No** (version recorded, never enforced) |

## Suggested priority order

1. **Fix the SQLitePCLRaw stack** (Infrastructure I1) — blocks the whole
   Infrastructure test suite and risks runtime failures in the not-yet-exercised
   Desktop.
2. **Fix the two failing Domain tests** (MSTest `DataRow` typing).
3. **Resolve the weight-without-catalogue FIXME** (Application A3) — an
   explicit open decision in the main inventory path.
4. **Implement the error-translation contract** (Application A1 + Domain D1/D2).
5. **De-duplicate AddStock/RemoveStock** (A2) *before* adding adjustment
   commands, so the third pipeline copy is not born.
6. **Concurrency/serialization guard** for the SQLite session (I2).
7. Cheap correctness wins: partner-name trim + unique index (A4/I5),
   partner-ID existence check (A5), schema-version enforcement (I3).
8. Then: history + adjustment use cases (A8), N+1 fix (A6), and the Desktop
   layer itself.
