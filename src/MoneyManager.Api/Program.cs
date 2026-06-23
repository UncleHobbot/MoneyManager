using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;
using MoneyManager.Api.Endpoints;
using MoneyManager.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults: OpenTelemetry, health checks, service discovery, HTTP resilience.
// OTLP export is inert unless OTEL_EXPORTER_OTLP_ENDPOINT is set (e.g. the dev dashboard).
builder.AddServiceDefaults();

// Add services
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

// EF Core — SQLite with context factory for per-operation contexts
builder.Services.AddDbContextFactory<DataContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Application services
builder.Services.AddScoped<DataService>();
builder.Services.AddScoped<AiProviderService>();
builder.Services.AddScoped<AIService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<TransactionQueryService>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddSingleton<DBService>();
builder.Services.AddSingleton<SettingsService>();

// CORS — allow Vite dev server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Initialize database — copy template if available, otherwise create schema via EF
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
var dbMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Data Source=(.+?)(?:;|$)");
if (dbMatch.Success)
{
    var dbPath = dbMatch.Groups[1].Value;
    if (!File.Exists(dbPath))
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath) ?? ".");
        var templatePath = Path.Combine(AppContext.BaseDirectory, "template", "MoneyManagerEmpty.db");
        if (File.Exists(templatePath))
        {
            File.Copy(templatePath, dbPath);
            app.Logger.LogInformation("Database initialized from template at {DbPath}", dbPath);
        }
    }

    // Ensure schema exists (handles both fresh DBs and empty files)
    using (var scope = app.Services.CreateScope())
    {
        var ctx = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<DataContext>>().CreateDbContextAsync();
        await ctx.Database.EnsureCreatedAsync();

        // For existing databases (e.g. migrated from legacy), create new tables that EnsureCreated skips
        await ctx.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS AiProviders (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                ProviderType TEXT NOT NULL,
                EncryptedApiKey TEXT NOT NULL,
                ApiUrl TEXT NOT NULL,
                Model TEXT NOT NULL,
                IsDefault INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
            )
            """);

        await ctx.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS Budgets (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CategoryId INTEGER NOT NULL,
                Amount TEXT NOT NULL,
                CONSTRAINT FK_Budgets_Categories_CategoryId FOREIGN KEY (CategoryId) REFERENCES Categories (Id)
            )
            """);
    }
}

// Warm the in-memory caches at startup
try
{
    using var scope = app.Services.CreateScope();
    var dataService = scope.ServiceProvider.GetRequiredService<DataService>();
    await dataService.WarmCacheAsync();
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Cache warm-up failed (empty database?) — will populate on first request");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Serve React static files in production
app.UseDefaultFiles();
app.UseStaticFiles();

// Minimal API endpoint groups
app.MapAccountEndpoints();
app.MapCategoryEndpoints();
app.MapTransactionEndpoints();
app.MapRuleEndpoints();
app.MapChartEndpoints();
app.MapImportEndpoints();
app.MapSystemEndpoints();
app.MapAIEndpoints();
app.MapBudgetEndpoints();

// SPA fallback — serve index.html for client-side routes, but never mask missing API routes.
app.MapFallback(async context =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    var webRootPath = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    var indexPath = Path.Combine(webRootPath, "index.html");
    if (!File.Exists(indexPath))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(indexPath);
}).WithMetadata(new HttpMethodMetadata([HttpMethods.Get, HttpMethods.Head]));

// Health check (used by the Docker HEALTHCHECK)
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Aspire default health endpoints (/health, /alive) — mapped in Development only.
app.MapDefaultEndpoints();

app.Run();
