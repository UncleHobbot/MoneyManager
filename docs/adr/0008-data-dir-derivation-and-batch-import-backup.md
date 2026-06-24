---
status: accepted
---

# Backup/CSV-archive dirs derive from the DB directory; backup moves from the import pipeline to batch orchestration

Two file-system concerns — database backups and imported-CSV archives — previously
resolved their locations from optional config keys (`BackupPath`, `CsvArchivePath`),
defaulting to `{AppContext.BaseDirectory}/...`. That default lands in `bin/.../` in
dev and in `/app/backups` (outside the mounted `/app/data` volume) in Docker, so
backups and archives were effectively ephemeral and lost on container recreation.
Separately, the import pipeline (`TransactionService.ImportAsync`) created a DB
backup before *every* imported file — fine for one-file-at-a-time uploads, but with
multi-file batch upload it would produce N backups per batch.

We decided:

1. **Derive both directories from the database file's directory.** A shared helper
   parses the SQLite connection string (`SqliteConnectionStringBuilder`), resolves
   the DB file's directory to an absolute path, and exposes it as the data dir.
   Backups go to `{dataDir}/backup`, CSV archives to `{dataDir}/imported`. In prod
   this is `/app/data/backup` and `/app/data/imported` (inside the mounted volume);
   in dev it is `../../data/backup` / `../../data/imported` next to the dev DB. The
   `BackupPath` and `CsvArchivePath` config keys are **removed** — the DB directory
   is the single source of truth, no overrides.

2. **Move the backup trigger out of the import pipeline into batch orchestration.**
   The frontend orchestrates a multi-file import as: one `POST /system/backup`, then
   the files uploaded **sequentially** (one request each, per-file result and error
   isolation). `ImportAsync` no longer calls `BackupAsync`; the pipeline no longer
   depends on `DBService`. If the pre-batch backup fails, the batch is **aborted**
   (no import without a rollback point).

## Considered options

- **Config-key overrides vs pure derivation.** Chose **pure derivation**. The user's
  requirement was "always `/app/data/backup`"; a single source of truth (the DB dir)
  removes the "why isn't the backup where I expected" failure mode and the dead
  `BackupPath` setting that was never wired to `DBService` anyway (it read
  `IConfiguration["BackupPath"]`, not the persisted `SettingsModel.BackupPath`).

- **Per-file backup (keep in pipeline) vs one backup per batch (orchestrated).**
  Chose **one per batch, orchestrated by the frontend**. Keeping the backup inside
  `ImportAsync` would create N backups per batch and quickly evict the kept-10
  history. A per-file `skipBackup` flag was considered but rejected as a leakier API
  than simply making "backup before an import session" an explicit caller step.
  This reverses the prior decision recorded in CONTEXT's "Bank Import Adapter" entry
  ("the pipeline owns ... backup").

- **Multi-file backend endpoint vs client-side loop.** Chose **client-side
  sequential loop** over the existing single-file endpoint. It keeps the backend
  unchanged for receipt/detection/archive, gives per-file results and failure
  isolation for free, and avoids server-side aggregation. Sequential (not parallel)
  because each import writes to SQLite and runs dedup; parallel writes risk locks for
  no real speedup in a personal app.

## Consequences

- `TransactionService` drops its `DBService` dependency; `ImportAsync` no longer
  backs up. Tests asserting backup-on-import move to assert the orchestration instead.
- Old backups/archives in the previous (ephemeral) locations are **not migrated** —
  they were already lost on redeploy; the path simply switches going forward.
- The dead `settings.json` layer (`SettingsService`, `GET/PUT /system/settings`,
  `SettingsModel`, `useSettings`/`useUpdateSettings`) is removed alongside this work,
  since its only two fields (`IsDarkMode`, `BackupPath`) were both unused.
