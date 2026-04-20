using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;
using MoneyManager.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
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

app.MapControllers();

// SPA fallback — serve index.html for client-side routes
app.MapFallbackToFile("index.html");

// Health check
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
