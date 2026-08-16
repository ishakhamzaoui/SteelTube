# SteelTube — Domain Layer Review

Date: 2026-08-13 · Scope: `src/SteelTube.Domain` (all 20 source files)
Build: compiles clean on `Debug|x64`. Tests: see "Test coverage" below.

## Verdict

The Domain layer is the strongest part of the codebase. It models the business
correctly (append-only operation ledger as source of truth, material identity =
Diameter + Thickness, signed quantities via `OperationType`), uses value objects
with construction-time invariant checks, and has exemplary documentation with
SAD/SRS traceability. The issues below are mostly about *consistency of the
exception model*, *small gaps in invariants*, and *UI formatting leaking into
the domain* — none are blocking for the current feature set.

## Strengths

- **Value objects as `readonly struct`s with factories** (`Length.cs:21`,
  `Diameter.cs:19`, `Thickness.cs:19`, `Weight.cs:19`, `KgPerMeter.cs:19`).
  Invariants ("always strictly positive") are enforced at construction, so
  invalid values cannot exist in the system.
- **`InventoryOperation` is a genuine immutable event.** Private setters, no
  mutating methods, `Rehydrate`/`Create` split
  (`InventoryOperation.cs:141`). The sign convention lives in exactly one
  place, `SignedLengthMeters` (`InventoryOperation.cs:54`), and the projection
  (`InventoryBalance.Apply`) reuses it — see
  `SqliteInventoryBalanceRepository.RebuildAllAsync`.
- **`OperationType` enum is meaningful**, not a naked flag; each value carries
  implicit direction (doc comment, `OperationType.cs:3-9`).
- **Exceptions are typed** into validation vs. business-rule
  (`Exceptions/DomainException.cs`).
- **Near-zero external dependencies** (BCL only) — the layer stays pure and
  trivially testable.
- **Documentation discipline.** Every type references its SAD/SRS section, which
  makes requirement traceability and future review much easier.

## Issues

### D1 — Domain exception types are declared but not used by the domain itself  [Medium]

`DomainValidationException` and `BusinessRuleViolationException`
(`Exceptions/DomainException.cs:20,30`) are documented as the way domain
invariants are reported, but the actual value objects and entities throw **BCL
exceptions**: `Guard.Positive` throws `ArgumentOutOfRangeException`
(`Guard.cs:14`), `Guard.NotNullOrWhiteSpace` throws `ArgumentException`
(`Guard.cs:26`). Only `SignedLengthMeters` uses `DomainValidationException`
(`InventoryOperation.cs:67`).

Consequence: the Application layer cannot reliably distinguish "the user typed
an invalid value" (a domain validation) from "a programming error", and the
documented error model (SAD 51) is not honored. The Application layer has to
catch `ArgumentOutOfRangeException`/`ArgumentException` directly (see
Application review A1), which is fragile.

Suggested direction: make `Guard` throw `DomainValidationException`, or map the
BCL exceptions once in a single Application-side translator.

### D2 — Inconsistent exception *types* inside Guard itself  [Low]

`Guard.Positive`/`NotNegative` use `ArgumentOutOfRangeException` while
`NotNullOrWhiteSpace`/`NotEmpty` use `ArgumentException` (`Guard.cs:11-33`).
The tests match this split (`LengthTests.cs:17` expects
`ArgumentOutOfRangeException`; `InventoryOperationTests.cs:41` expects
`ArgumentException`), so it is deliberate — but it is an accidental-looking
inconsistency that makes the catch story harder.

### D3 — Presentation formatting leaks into the domain  [Low]

`TubeSpecification.DisplayName` (`TubeSpecification.cs:61`) and every
value-object `ToString()` use culture-sensitive `decimal` interpolation
(`{Millimeters:0.##}`). On a comma-decimal Windows locale, `1.5` renders as
`1,5`, which will be stored/displayed inconsistently across machines and
exported documents. The domain should either not format at all or use the
invariant culture; formatting belongs in the Desktop layer.

### D4 — `Rehydrate` bypasses more validation than documented  [Low]

`InventoryOperation.Rehydrate` (`InventoryOperation.cs:141`) skips the
adjustment-note rule (intentional and documented) **and** the `PieceCount`
non-negativity check that `Create` performs (`InventoryOperation.cs:112-113`).
A corrupt/legacy row with a negative piece count would hydrate silently. At
least an `else` assert-style check would fail fast on bad data.

### D5 — Value objects are not uniformly comparable  [Low]

`Length` and `Weight` implement `IComparable<T>` and full comparison operators;
`Diameter`, `Thickness`, `KgPerMeter` implement only equality
(`Length.cs:12,30`, `Weight.cs:10,28`, `Diameter.cs:10`, `KgPerMeter.cs:10`).
Harmless today (SQL handles ordering via `CAST(... AS REAL)`), but the
inconsistency is a smell and `IComparable` on dimensions would enable generic
range utilities later.

### D6 — `WeightCatalogueEntry.UpdateFactor` has no change detection / versioning  [Low]

`UpdateFactor` (`WeightCatalogueEntry.cs:55`) unconditionally overwrites the
factor and timestamp, even if the caller passes the identical value. Combined
with the planned multi-device sync, catalogue edits will need last-writer-wins
or a version column to merge cleanly. Worth designing now since the sync phase
(SAD 30-32) is on the roadmap.

### D7 — No invariant on operation date sanity  [Low]

`InventoryOperation.Create` accepts any `operationDate`
(`InventoryOperation.cs:105`). Nothing forbids a far-future date, which would
distort `ORDER BY OperationDate` history queries. Application-side UI can clamp
it, but the domain currently has no opinion.

## Test coverage

`SteelTube.Domain.Tests` covers `Length` factories, the two
`WeightConversionService` math examples from SRS 7.2/7.3, and basic
`InventoryOperation` sign rules + note requirement
(`tests/SteelTube.Domain.Tests/*`).

Gaps:
- No tests for `Diameter`, `Thickness`, `KgPerMeter`, `Weight` factory guards.
- No tests for `BusinessPartner`, `TubeSpecification`,
  `WeightCatalogueEntry.UpdateFactor`, `InventoryBalance.Apply`.
- No tests for the `Rehydrate` paths or the exception taxonomy.
- `LengthTests.FromMeters_rejects_non_positive_values` currently **fails**: the
  `[DataRow(0)]`/`[DataRow(-5)]` literals are `int`, but the parameter is
  `decimal`, and MSTest 4.x no longer performs implicit numeric conversion
  ("Test data doesn't match method parameters"). Verified by running the test
  suite: 2 of 7 domain tests fail for this reason.

## Recommendation summary

1. Align `Guard` + value objects on the `DomainException` taxonomy (D1/D2) —
   this unblocks the Application error-translation contract.
2. Fix the `LengthTests` `DataRow` typing.
3. Move display formatting out of the domain (D3).
4. Decide and document whether `Rehydrate` should assert `PieceCount >= 0`
   (D4).
5. Design catalogue-entry versioning before the sync phase (D6).
