---
status: accepted
---

# Adopt .NET Aspire for dev-time orchestration only

MoneyManager is a single ASP.NET Core API that serves a React/Vite SPA, backed by a
SQLite file and shipped as one Docker image. We adopt .NET Aspire to improve the
**local dev loop** (one command starts the API + Vite dev server with the Aspire
dashboard) and to add **OpenTelemetry instrumentation, health checks, and HTTP
resilience** via a shared ServiceDefaults project — but Aspire stays out of the
production deployment, which keeps its existing Dockerfile → GHCR → compose pipeline.

## Considered options

- **Aspire-generated deploy artifacts (`aspire publish`)** — rejected. For a SQLite
  single-image app it would split the SPA into its own container (a 2-container
  re-architecture) and would not reproduce the `data/` volume mount or the
  `docker-entrypoint.sh` template-seeding, which Aspire doesn't model.
- **Re-architect to multi-container / Azure Container Apps** — rejected. No driver
  for it today; SQLite is a stateful file, not a cloud-native resource.
- **Migrate SQLite → Postgres to fit Aspire's resource model** — rejected. Out of
  scope; the file DB is intentional for this app.
- **AppHost-only, no ServiceDefaults reference** — rejected. Telemetry would exist
  only while the dev dashboard runs; we want instrumentation present in prod builds too.

## Consequences

- New projects: `MoneyManager.AppHost` (dev-time orchestrator) and
  `MoneyManager.ServiceDefaults`; the API references ServiceDefaults and calls
  `AddServiceDefaults()`.
- The **production Docker build must copy/restore the ServiceDefaults project** (the
  runtime image stays a single container; only the build inputs change).
- The AppHost pins the API to a fixed `:5000` and launches the SPA via `AddNpmApp`,
  so the existing Vite `/api → :5000` proxy keeps working with **no frontend changes**.
- SQLite is not an Aspire resource — it remains a connection string; volume mounting
  and template-seeding stay in the Dockerfile/compose as today.
- OTEL is instrumented but **not exported in production** until
  `OTEL_EXPORTER_OTLP_ENDPOINT` is pointed at a backend (deferred).
