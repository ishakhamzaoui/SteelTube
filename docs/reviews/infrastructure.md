# SteelTube — Infrastructure Layer Review

Date: 2026-08-13 · Scope: `src/SteelTube.Infrastructure` (all 16 source files)
Build: compiles clean on `Debug|x64`.

## Verdict

The infrastructure is coherent and admirably simple for the target: one shared
SQLite connection, WAL + foreign keys on, an ambient transaction unit of work,
parameterized SQL everywhere, and a clean manual composition root. Decimal
precision is handled carefully (decimal-as-text). The two serious problems are
(1) a **broken SQLitePCLRaw dependency pairing that makes the Infrastructure
test suite fail on startup**, and (2) the **ambient `CurrentTransaction`
mechanism has no thread-affinity/serialization guarantees** for a connection
type that is explicitly not thread-safe. Several repository methods exist that
no use case calls yet (sync/history scaffolding).

## Strengths

- **Single long-lived connection** (`SqliteSession.cs:22-48`) with
  `PRAGMA journal_mode = WAL; PRAGMA foreign_keys = ON;` — the right calls for a
  single-user offline desktop app.
- **Ambient-transaction unit of work** (`SqliteUnitOfWork.cs:25-62`) that
  nests correctly (joins an active transaction instead of double-committing)
  and always clears `CurrentTransaction` in `finally`.
- **`SqlConvert` is a single, well-documented place** for the decimal/guid/date
  <-> TEXT mapping (`SqlConvert.cs`), with a deliberate, narrow REAL-comparison
  exception for equality lookups (`SqlConvert.ToLookupDouble`, `SqlConvert.cs:52`).
- **`RebuildAllAsync` reuses `InventoryOperation.SignedLengthMeters`**
  (`SqliteInventoryBalanceRepository.cs:73-103`) instead of re-implementing the
  sign convention in SQL — the convention lives in exactly one place.
- **All SQL is parameterized**; string building only ever concatenates fixed
  column names / where clauses (`SqliteInventoryOperationRepository.cs:67-107`).
- **Composition root** with proper disposal on startup failure
  (`CompositionRoot.cs:102-129`), no DI container, per SAD Goal 5.

## Issues

### I1 — SQLitePCLRaw version mismatch; Infrastructure test suite is red  [High]

`Microsoft.Data.Sqlite` 10.0.11 depends on `SQLitePCLRaw.core` **2.1.12.3116**,
but the project pins and redirects to `SQLitePCLRaw.core` **3.0.5** (see
`SteelTube.Infrastructure.csproj:62-73`, `packages.config`, and the binding
redirects in `Infrastructure.Tests/app.config:5-8` and `Desktop/App.config:12-15`).
Only the 3.0.5 assembly exists in `packages\` — there is no 2.1.12.3116 build
anywhere.

Verified by running the test suite:
```
Failed AddStock_then_RemoveStock_produces_expected_running_total
  ... System.IO.FileNotFoundException: Could not load file or assembly
  'SQLitePCLRaw.core, Version=2.1.12.3116, ...'
```
All three `CompositionRootTests` fail at `Setup` for this reason. The desktop
app's `App.config` redirect may mask the problem at runtime (redirecting
2.1.12.3116 → 3.0.5.3129), but even then Microsoft.Data.Sqlite 10 was built
against the 2.1 API surface; running against 3.0.5 is unsupported and can throw
`MissingMethodException`/`TypeLoadException` in paths not yet exercised.

There is also a confusing package mix: `packages.config` lists both
`SQLite` 3.53.4 (System.Data.SQLite) **and** `SQLitePCLRaw.lib.e_sqlite3` +
`bundle_e_sqlite3` + `config.e_sqlite3`, with the csproj importing
`SQLite.props`/`SQLite.targets` but never referencing `System.Data.SQLite`
itself. Two generations of native providers are being pulled in.

Suggested fix: pick one coherent stack — e.g. Microsoft.Data.Sqlite 10.x with
its matching `SQLitePCLRaw.core`/`bundle_e_sqlite3` 2.1.x chain, or drop
Microsoft.Data.Sqlite 10 in favor of the version compatible with raw 3.0.5 —
then remove the orphan `SQLite` package and align all `app.config` redirects.

### I2 — `CurrentTransaction` is shared mutable state without thread guarantees  [Medium]

`SqliteSession.CurrentTransaction` is a plain mutable property
(`SqliteSession.cs:20`) set by `SqliteUnitOfWork` (`SqliteUnitOfWork.cs:45`).
`Microsoft.Data.Sqlite` connections are **not thread-safe**, yet nothing
prevents two use cases from running concurrently on different threads and
interleaving: repo A would see repo B's ambient transaction and join it, or
observe `null` mid-commit. Today the Desktop is single-threaded so it is safe in
practice, but the invariant is implicit.

Suggested direction: either document/enforce "all repository access is
single-threaded", or make the transaction ambient per-execution (e.g. an
`AsyncLocal`/scoped `ISqliteConnectionProvider` snapshot) so a handler always
sees its own transaction, and add a simple reentrancy guard on
`ExecuteInTransactionAsync`.

### I3 — Schema version is recorded but never enforced  [Medium]

`DbInitializer` inserts `SchemaVersion` on first run (`DbInitializer.cs:99-112`)
and never checks it afterward. If a newer build is run against an older DB (or
vice versa), `EnsureCreatedAsync` silently continues on the mismatched schema —
there is no migration path yet, only scaffolding (SAD 44). At minimum, compare
the stored version against `CurrentSchemaVersion` and fail loudly on mismatch.

### I4 — `RebuildAllAsync` materializes the whole ledger in memory and rewrites row-by-row  [Medium]

`SqliteInventoryBalanceRepository.cs:73-103` reads every operation into a
`List`, groups in memory, then does one `DELETE` + one `INSERT` per balance.
This is correct and reuses domain code, but at larger histories it is O(ledger)
memory plus O(balances) statements. A single `INSERT ... SELECT` with a
`SUM(CASE ...)` would be O(balances) and keep the delete+insert atomicity. Only
worth changing when history grows.

### I5 — No DB-level uniqueness on `BusinessPartners.Name`  [Medium]

`SqliteBusinessPartnerRepository.cs` has no unique index on `Name` (the lookup
uses `COLLATE NOCASE`, `SqliteBusinessPartnerRepository.cs:41`). The
Application-layer find-then-insert is not race-safe, and combined with the
trim-on-insert-only bug (Application review A4) duplicates are easy to produce.
A `CREATE UNIQUE INDEX ... ON BusinessPartners(Name COLLATE NOCASE)` would make
duplicates impossible and simplify `GetOrCreateByNameAsync` to
`INSERT ... ON CONFLICT DO NOTHING` + re-read (same pattern as
`SqliteTubeSpecificationRepository.GetOrCreateAsync`).

### I6 — `GetHistoryAsync` Take/Skip are unvalidated  [Low]

`SqliteInventoryOperationRepository.cs:103-105` binds `filter.Take`/`Skip`
directly. In SQLite a negative `LIMIT` means *unlimited*; a caller bug (e.g.
`Take = -1`) silently returns the whole table. Clamp to sane bounds.

### I7 — `NextSequenceNumberAsync` atomicity depends on the caller  [Low]

`SqliteDeviceContext.NextSequenceNumberAsync` is UPDATE-then-SELECT
(`SqliteDeviceContext.cs:61-80`); it is atomic only because every current
caller runs inside a unit-of-work transaction. If ever called outside a
transaction (or concurrently), the counter could be misread. Either move it into
the transaction contract explicitly or make it a single
`UPDATE ... RETURNING LastSequenceNumber` (SQLite ≥3.35).

### I8 — `PieceCount` unchecked int cast on read  [Low]

`SqliteInventoryOperationRepository.cs:174` does `(int)reader.GetInt64(6)` —
a corrupt row with `PieceCount > int.MaxValue` would silently wrap (unchecked
cast). Guard the range.

### I9 — REAL equality lookup couples .NET and SQLite double parsing  [Low]

`SqlConvert.ToLookupDouble` (`SqlConvert.cs:52`) converts the user's decimal to
`double`, and SQL compares `CAST(DiameterMm AS REAL) = $diameter`. Both sides
should round identically for realistic dimension values, but this is exactly the
class of "text stored, double compared" coupling that SAD 60 wanted to avoid.
If a row is ever inserted via a different path that produces a different text
form (e.g. `"25.40"` vs `"25.4"`), the comparison still works (both cast to the
same real), but a simpler and fully-exact alternative is matching on the
normalized invariant string. Keep as-is if the lookup values are always
produced through `SqlConvert.ToText`.

### I10 — `SqliteDeviceContext.CreateAsync` is not race-safe for a double-install  [Low]

If two processes initialize the same fresh database simultaneously, both pass
the `SELECT ... LIMIT 1`, then the second `INSERT` fails on the PK
(`SqliteDeviceContext.cs:35-58`). Startup is effectively single-threaded today,
so this is theoretical.

### I11 — Implemented-but-unused repository surface  [Info]

`GetByIdAsync` (tube specs, business partners), `GetHistoryAsync`,
`GetByOriginDeviceAfterSequenceAsync`, `GetAllForTubeSpecificationAsync`, and
`RebuildAllAsync` are fully implemented but no use case calls them
(verified by search — no Application-layer consumer). This is intentional
scaffolding for the sync/history phases (SAD 29/33, SRS 10/12), but it is dead
code today and therefore untested by any behavior.

### I12 — `RebuildAllAsync` uses `DateTime.UtcNow` instead of `IClock`  [Info]

`SqliteInventoryBalanceRepository.cs:86` — minor consistency nit; the rest of
the stack goes through `IClock`.

## Test coverage

`SteelTube.Infrastructure.Tests` contains three end-to-end
`CompositionRootTests` (add→remove running total, spec reuse, negative-stock
flag). They are currently **all failing at `Setup`** because of I1. When the
dependency is fixed, note the coverage gaps:

- No test for `GetCurrentStock` enrichment (weight column, ordering, negative
  exclusion).
- No test for catalogue add/update/duplicate handling or partners.
- No test for `RebuildAllAsync` reconstruction (the SAD 22 guarantee).
- No test for `NextSequenceNumberAsync` monotonicity or `SqliteUnitOfWork`
  rollback behavior.

## Recommendation summary

1. Fix the SQLitePCLRaw stack (I1) — this is blocking the entire Infrastructure
   test suite.
2. Address thread-safety of the ambient transaction (I2) before the Desktop
   starts doing async work.
3. Add schema-version enforcement (I3) and a `BusinessPartners.Name` unique
   index (I5) — both cheap and prevent future data-corruption classes.
4. Clamp paging bounds (I6) and make the sequence increment robust (I7).
