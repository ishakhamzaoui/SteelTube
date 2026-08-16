# SteelTube — Application Layer Review

Date: 2026-08-13 · Scope: `src/SteelTube.Application` (all 37 source files)
Build: compiles clean on `Debug|x64`.

## Verdict

The layer has a clean command/query-per-feature shape, constructor DI, atomic
units of work, and well-isolated DTOs. The dominating problems are (a) the
error/exception contract is only half-implemented, (b) `AddStock`/`RemoveStock`
are ~80% duplicated code, and (c) several features modeled in Domain (adjustment
operations, history) are not wired up here. There is **zero test coverage** for
this layer.

## Strengths

- **Clear use-case organization** — one folder per feature, handler +
  command + result per use case.
- **Unit-of-work discipline**: every mutating handler runs inside
  `ExecuteInTransactionAsync` so ledger + projection stay atomic
  (`AddStockCommandHandler.cs:52`, `RemoveStockCommandHandler.cs:52`,
  `AddCatalogueEntryCommandHandler.cs:23`, `CreatePartnerCommandHandler.cs:22`).
- **Domain is not leaked through DTOs** — handlers map entities to
  DTOs/Results (`GetCatalogueQueryHandler.cs:21-30`).
- **Nullable driving-quantity pattern** for the Length/Weight toggle is
  validated early (`AddStockCommandHandler.cs:57-60`).
- **Result types are honest** — `RemoveStockResult.ResultsInNegativeStock`
  surfaces a business-validity concern to the UI instead of hiding it
  (`RemoveStockResult.cs:18`).
- Thorough SAD/SRS-referencing docs.

## Issues

### A1 — Error taxonomy is incomplete; raw exceptions reach the UI  [High]

Only `UseCaseValidationException` exists (`Common/UseCaseException.cs:17`).
Everything else propagates uncaught:

- Value-object factories throw BCL exceptions: `Length.FromMeters` throws
  `ArgumentOutOfRangeException` for a negative length (`AddStockCommandHandler.cs:75`
  → `Domain/ValueObjects/Length.cs:23`). A negative user input therefore leaks
  an unhandled `ArgumentOutOfRangeException` to the Desktop layer.
- `BusinessRuleViolationException` (a **Domain** type) is thrown directly from
  the handler when weight is given without a catalogue entry
  (`AddStockCommandHandler.cs:87`, `RemoveStockCommandHandler.cs:87`),
  contradicting the documented contract that "the Application layer translates
  Domain exceptions into user-friendly results" (SAD 51/52).
- There is no "not found" concept: `UpdateCatalogueEntryCommandHandler.cs:31`
  reports a missing entry as a *validation* error.

Suggested direction: complete the `UseCaseException` family
(validation / business-rule / not-found / infrastructure) and add one translator
that maps `DomainException`, `ArgumentException`, and
`ArgumentOutOfRangeException` to the right use-case exception.

### A2 — `AddStock` and `RemoveStock` handlers are ~80% duplicated  [High]

Compare `AddStockCommandHandler.cs` with `RemoveStockCommandHandler.cs`: the
whole validate → resolve spec → resolve quantity → resolve partner → sequence →
create operation → apply balance pipeline is copy-pasted, including the
identical `ResolvePartnerAsync` helper and the identical FIXME. The only
differences are `CreatePurchase` vs `CreateSale` and the negative-stock flag in
the result.

Risk: the two files will drift (the `GetById`/existence-check fix from A4/A5
has to be applied twice; a future rule change like "block overselling" would
touch both). Extract a shared stock-command pipeline or a common
base/service, parameterized by operation factory + validation rules.

### A3 — The FIXME: weight-input without a catalogue entry  [High]

Both handlers carry:
`// FIXME: If the weight is provided but no catalogue entry exists, we should still allow the operation to proceed...`
(`AddStockCommandHandler.cs:84-85`, `RemoveStockCommandHandler.cs:84-85`).

The comment and the code disagree — the code throws
`BusinessRuleViolationException`. This is a deliberate open decision (likely
"store weight-only operation with `Length`/`WeightPerMeterUsed` unknown"). It
needs to be resolved: either implement the weight-only path (persist the
operation with `Length` unavailable — which conflicts with `Length` being a
mandatory positive domain value, so this needs a domain decision first), or
delete the FIXME and keep the hard block. As-is, it reads as unfinished.

### A4 — Implicit partner creation trims on insert but not on lookup  [Medium]

`BusinessPartner.Create` trims the name (`BusinessPartner.cs:28`), but
`GetOrCreateByNameAsync` searches with the **untrimmed** input
(`SqliteBusinessPartnerRepository.cs:55`). In the use case, the raw
`command.BusinessPartnerName` is passed straight through
(`AddStockCommandHandler.cs:124-127`). Typing `"  ACME  "` once creates
partner `"ACME"`; typing `"  ACME  "` again finds nothing and creates a second
`"ACME"`. Duplicate partners are the result. Fix: trim/normalize the name in
`ResolvePartnerAsync` (or in the repository) before lookup.

### A5 — `BusinessPartnerId` is trusted without existence check  [Medium]

`ResolvePartnerAsync` returns `command.BusinessPartnerId` verbatim
(`AddStockCommandHandler.cs:121-122`). A stale or invalid ID hits the SQLite
FOREIGN KEY constraint and surfaces as a raw `SqliteException`. The repository
already exposes `GetByIdAsync` (`IBusinessPartnerRepository.cs:14`) but no use
case calls it. Verify the ID exists (and arguably that `IsProvider`/`IsCustomer`
matches the operation) before persisting, and translate the miss into a
friendly error.

### A6 — N+1 catalogue lookups in `GetCurrentStock`  [Medium]

`GetCurrentStockQueryHandler.cs:46` calls `_catalogue.FindAsync(...)` once per
balance inside the loop. With N materials that is N extra round-trips.
`IWeightCatalogueRepository` has no `GetAllAsync`-style dictionary lookup used
here; a single `GetAllAsync` + in-memory dictionary would collapse it to one
query. Fine today at small N, but it is the first thing that will hurt when
spec counts grow.

### A7 — `RemoveStock` creates a specification for never-stocked material  [Medium]

`RemoveStockCommandHandler.cs:66` calls `GetOrCreateAsync`, so selling a
material that was never purchased inserts a `TubeSpecification` and a *negative*
`InventoryBalance` row. This matches the "record overselling, flag it"
philosophy (SAD 36/37) but silently creates ghost rows. Consider surfacing
"this specification has never been stocked" in the result so the UI can warn,
rather than only showing a negative balance.

### A8 — Feature gaps: modeled in Domain, absent here  [Medium]

- **Adjustment operations**: `InventoryOperation.CreateAdjustmentIncrease` /
  `CreateAdjustmentDecrease` exist in Domain (with the mandatory-note rule),
  but there are **no command handlers** for them. Only Purchase (AddStock) and
  Sale (RemoveStock) are reachable.
- **Operation history**: `IInventoryOperationRepository.GetHistoryAsync`
  (`IInventoryOperationRepository.cs:21`) is implemented in Infrastructure but
  no query/use case consumes it — the SRS history screen cannot be built yet.
- **Partner editing**: `BusinessPartner.Rename`/`SetRoles`
  (`BusinessPartner.cs:58,65`) have no commands.
- **Catalogue delete**: not modeled at all (may be intentional).

These are roadmap gaps, but they mean the Domain surface overstates what the
product can currently do.

### A9 — Minor robustness items  [Low]

- `OperationDate` is not range-checked; a far-future date reorders history
  (`AddStockCommand.cs:28`, used at `AddStockCommandHandler.cs:100`).
- `Note` and `Name` have no length limits.
- `GetCatalogue`/`GetPartners`/`GetCurrentStock` are unpaged
  (acknowledged in the query docs).
- `AddStockResult`/`RemoveStockResult` are near-duplicates
  (`AddStockResult.cs`, `RemoveStockResult.cs`).

## Test coverage

`SteelTube.Application.Tests` contains **only `AssemblyInfo.cs`** — no test
classes at all, despite Moq/Castle.Core/FluentAssertions being referenced.
The application layer's rules (length/weight exclusivity, weight-without-entry
behavior, partner implicit creation, negative-stock flag, catalogue
add/update validation) are entirely untested at the unit level; the only
exercises are the Infrastructure end-to-end tests, which are themselves
currently red (see Infrastructure review I1).

## Recommendation summary

1. Resolve A3 (weight-without-catalogue decision) — it is the flagged open item.
2. Implement the error-translation contract (A1).
3. De-duplicate AddStock/RemoveStock (A2) — do this before adding adjustments
   (A8), because the adjustment handlers would otherwise be a third copy.
4. Fix partner trimming (A4) and partner-ID verification (A5).
5. Collapse the N+1 in GetCurrentStock (A6).
6. Add real unit tests for the handlers.
