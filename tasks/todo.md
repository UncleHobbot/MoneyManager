# CSV multi-import + backup/path management plan

Outcome of the `/grill-with-docs` session (2026-06-24). Design is captured in
`CONTEXT.md` ("Bank Import Adapter", new "Database backup") and ADR-0008. This file
is the sequenced build plan.

## Goal

1. Allow uploading multiple CSV files at once (not one-by-one).
2. Archive each imported CSV into `{dataDir}/imported`.
3. Always store DB backups in `{dataDir}/backup`; remove the Backup Path setting.
4. Remove the dead Dark Mode setting (theme is the top-right toggle).

`dataDir` = the directory of the SQLite DB file from the connection string
(prod `/app/data`, dev `../../data`). Pure derivation, no config override.

---

## Phase 1 — Backend: data-dir derivation + path wiring

- [ ] Add a shared helper (e.g. `Helpers/DataPaths.cs`) that resolves the DB file's
      directory from `IConfiguration` via `SqliteConnectionStringBuilder`
      (`ConnectionStrings:DefaultConnection`), returned as an absolute path.
      → verify: unit test resolves both `/app/data/...` and `../../data/...` forms.
- [ ] `DBService.GetBackupPath()` → `{dataDir}/backup`; drop `IConfiguration["BackupPath"]`.
- [ ] `ImportEndpoints.GetArchivePath()` → `{dataDir}/imported`; drop `IConfiguration["CsvArchivePath"]`.
      → verify: backup + archive land under the data dir, not `bin/.../`.

## Phase 2 — Backend: archive filename uniqueness

- [ ] Change archive filename to `{yyyy-MM-dd HHmmss} {BankType} {sanitized-original}.csv`.
- [ ] Sanitize the original filename (strip path separators / `..` / invalid chars).
      → verify: two same-bank uploads in one day produce two distinct files.

## Phase 3 — Backend: backup out of the import pipeline

- [ ] Remove `await dbService.BackupAsync()` from `TransactionService.ImportAsync`
      (TransactionService.cs:51).
- [ ] Remove the now-unused `DBService dbService` ctor dependency from `TransactionService`.
      → verify: solution builds; pipeline no longer references DBService.

## Phase 4 — Backend: delete the dead settings.json layer

- [ ] Delete `SettingsService`, `Model/SettingsModel.cs`, and the `GET/PUT /system/settings`
      endpoints (+ DI registration in Program.cs).
- [ ] Remove the `SettingsPath` config key usage (gone with SettingsService).
      → verify: no references to SettingsService / SettingsModel remain; build passes.

## Phase 5 — Backend tests

- [ ] Update `TransactionServicePipelineTests` / `TransactionServiceImportTests` — drop
      backup-on-import assertions; construct `TransactionService` without `DBService`.
- [ ] Update any import-endpoint tests that set `CsvArchivePath` / `BackupPath` config.
- [ ] Add test for the new archive filename format + sanitization.
      → verify: `dotnet test src/MoneyManager.Api.Tests` green.

## Phase 6 — Frontend: multi-file upload

- [ ] ImportPage: `input multiple`; drop-zone accepts all `.csv` from `dataTransfer.files`;
      state `selectedFiles: File[]`; render the selected-file list with per-file remove.
- [ ] Upload flow: `POST /system/backup` once → on failure abort with error; else upload
      files **sequentially**, tracking per-file status (pending → done/error).
- [ ] Replace the single `ImportResult` block with a per-file results list + an aggregate
      summary (total imported / skipped).
- [ ] Keep one batch-wide Bank Type dropdown (Auto default) + Create-accounts checkbox.
      → verify (vitest + manual): selecting 3 CSVs imports all three; one failing file
      does not abort the others; a failed pre-batch backup blocks the batch.

## Phase 7 — Frontend: backups on Settings, remove dead settings

- [ ] Move backup management (list + Restore w/ confirm + Create + Cleanup) from the
      orphaned `BackupPage.tsx` into a card on SettingsPage; delete `BackupPage.tsx`.
- [ ] Remove the Dark Mode + Backup Path controls and the Save-Settings flow from
      SettingsPage; remove `useSettings`/`useUpdateSettings` hooks and the
      `SettingsModel` type (+ `backupPath`/`isDarkMode` fields).
      → verify (vitest + manual): Settings shows AI Providers + Backups; no dark-mode
      or backup-path field; theme toggle still works via the header button.

## Phase 8 — Verify whole

- [ ] Backend: `dotnet build` + `dotnet test src/MoneyManager.Api.Tests`.
- [ ] Frontend: `npm run build`, `npm run lint`, `npm test` (in `src/moneymanager-web`).
- [ ] Manual smoke: multi-file import → files in `data/imported`, one backup in
      `data/backup`, restore works from Settings.

---

## Review

Implemented 2026-06-24. All 8 phases done.

**Backend**
- `Helpers/DataPaths.cs` resolves the data dir from the connection string; `DBService`
  and `ImportEndpoints` derive `{dataDir}/backup` and `{dataDir}/imported`. Config keys
  `BackupPath` / `CsvArchivePath` removed.
- Archive filename → `{yyyy-MM-dd HHmmss} {BankType} {sanitized-original}.csv`;
  `SanitizeForFileName` strips path separators + illegal chars.
- Backup removed from `TransactionService.ImportAsync`; `DBService` dependency dropped.
- Dead settings layer deleted: `SettingsService`, `SettingsModel`, `GET/PUT /system/settings`,
  DI registration.

**Frontend**
- ImportPage: `multiple` input + multi-drop, selected-file list with remove, one
  `POST /system/backup` before the batch (aborts on failure), sequential per-file upload
  with per-file status + aggregate summary.
- SettingsPage: dropped Dark Mode + Backup Path; added a Backups card (list + Restore +
  Create + Cleanup) merged from the orphaned `BackupPage.tsx`, which was deleted.
- Removed `useSettings`/`useUpdateSettings` hooks + `SettingsModel` type.

**Verification**
- Backend: `dotnet test` → 230 passed (incl. new `ImportEndpointsArchiveTests`).
- Runtime: `POST /api/system/backup` lands the file in `data/backup` (path derivation
  confirmed end-to-end).
- Frontend: `npm run lint` clean, `npm run build` succeeds, `npm test` → 88 passed.
- Browser smoke via preview MCP not completed — a second Vite instance wouldn't bind a
  port alongside the user's running dev server; relied on the production build + vitest +
  the live API check instead.

**Notes / not done**
- Backup retention is still manual (Cleanup button) — auto-cleanup intentionally out of scope.
- No migration of old backups/archives from the previous ephemeral locations (by decision).
