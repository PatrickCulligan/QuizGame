using System;
using System.Linq;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using QuizGame.Data;
using QuizGame.Hubs;
using QuizGame.Services;

var builder = WebApplication.CreateBuilder(args);

// Structured logging - write to console (k8s will capture)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    });
builder.Services.AddAuthorization();

// Database: use IDbContextFactory and prefer Postgres in prod, fallback to Sqlite for local/dev
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(conn) && conn.Contains("Host=", StringComparison.OrdinalIgnoreCase))
    {
        // Postgres style connection string detected
        options.UseNpgsql(conn, npgsql => npgsql.EnableRetryOnFailure());
    }
    else
    {
        options.UseSqlite(conn ?? "Data Source=quizgame.db");
    }
});

// Standard services
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddHttpClient<OpenAiQuizService>();
builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<LeaderboardService>();

// Health checks - include DB check
builder.Services.AddHealthChecks()
       .AddCheck<QuizGame.Services.HealthChecks.DbHealthCheck>("db", tags: new[] { "ready" });

var app = builder.Build();

// CLI-style explicit migration: run with `--migrate` (used by CI or k8s Job)
if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase))
{
    Log.Information("Migration mode requested. Applying EF Core migrations...");
    try
    {
        using var scope = app.Services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var db = dbFactory.CreateDbContext();
        db.Database.Migrate();
        Log.Information("Migrations applied successfully.");
        return;
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Failed to apply migrations.");
        throw;
    }
}

// For local Docker and SQLite-based runs, make normal startup self-initializing.
// Postgres environments should keep using the explicit migration step.
if (!string.IsNullOrWhiteSpace(conn) && !conn.Contains("Host=", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var db = dbFactory.CreateDbContext();
        db.Database.Migrate();
        Log.Information("SQLite migrations applied during app startup.");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Failed to apply SQLite migrations during app startup.");
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

 app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");
app.MapRazorPages();
app.MapHub<GameHub>("/gamehub");

app.Run();
