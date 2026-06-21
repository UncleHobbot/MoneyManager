var builder = DistributedApplication.CreateBuilder(args);

// ASP.NET Core API.
// Pinned to :5000 and run unproxied so the React dev server's existing Vite proxy
// (/api -> http://localhost:5000) keeps working with no frontend changes.
// SQLite stays a plain connection string (appsettings) — it is not an Aspire resource.
// launchProfileName: null disables launchSettings (so we control the port), so the
// Development environment it used to set must be re-applied explicitly — otherwise the
// API falls back to Production config and the prod connection string.
var api = builder.AddProject<Projects.MoneyManager_Api>("api", launchProfileName: null)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpEndpoint(name: "http", port: 5000, isProxied: false);

// React + Vite dev server — runs `npm run dev`, listening on Vite's default :5173.
builder.AddNpmApp("web", "../moneymanager-web", "dev")
    .WaitFor(api)
    .WithHttpEndpoint(name: "http", port: 5173, isProxied: false)
    .WithExternalHttpEndpoints();

builder.Build().Run();
