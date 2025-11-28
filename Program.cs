using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;
using System;
using System.Runtime.Intrinsics.X86;
using TodoApp.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Create a logger for startup
var startupLogger = LoggerFactory.Create(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
}).CreateLogger("Startup");

startupLogger.LogInformation("=== Application Starting ===");
startupLogger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);
startupLogger.LogInformation("Content Root: {ContentRoot}", builder.Environment.ContentRootPath);

// Log port configuration
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
startupLogger.LogInformation("Port from environment: {Port}", port);

// Get connection string - prefer environment variable, fallback to appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

startupLogger.LogInformation("Connection string found: {HasConnectionString}", !string.IsNullOrEmpty(connectionString));
if (!string.IsNullOrEmpty(connectionString))
{
    // Log connection string with password masked
    var maskedConnectionString = MaskPassword(connectionString);
    startupLogger.LogInformation("Connection string (masked): {ConnectionString}", maskedConnectionString);
}
else
{
    startupLogger.LogError("CRITICAL: No connection string configured!");
    throw new InvalidOperationException("Database connection string is not configured! Set ConnectionStrings__DefaultConnection environment variable or update appsettings.json");
}

// Configure services
startupLogger.LogInformation("Configuring DbContext with Npgsql...");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    options.EnableDetailedErrors(builder.Environment.IsDevelopment());
});

startupLogger.LogInformation("Configuring controllers and OpenAPI...");
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "Todo API";
    config.Version = "v1";
});

var app = builder.Build();

// Log all configuration keys (for debugging)
startupLogger.LogInformation("=== Configuration Keys ===");
foreach (var config in builder.Configuration.AsEnumerable())
{
    var value = config.Value;
    if (config.Key.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
        config.Key.Contains("Secret", StringComparison.OrdinalIgnoreCase))
    {
        value = "***MASKED***";
    }
    startupLogger.LogInformation("Config: {Key} = {Value}", config.Key, value ?? "(null)");
}
startupLogger.LogInformation("=== End Configuration Keys ===");

// Run migrations on startup
startupLogger.LogInformation("Running database migrations...");
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        startupLogger.LogInformation("Testing database connection...");
        await db.Database.CanConnectAsync();
        startupLogger.LogInformation("Database connection successful!");

        startupLogger.LogInformation("Applying pending migrations...");
        await db.Database.MigrateAsync();
        startupLogger.LogInformation("Migrations completed successfully!");

        var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
        var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();

        startupLogger.LogInformation("Applied migrations count: {Count}", appliedMigrations.Count());
        startupLogger.LogInformation("Pending migrations count: {Count}", pendingMigrations.Count());
    }
}
catch (Exception ex)
{
    startupLogger.LogError(ex, "CRITICAL: Failed to connect to database or run migrations!");
    throw;
}

startupLogger.LogInformation("Configuring middleware pipeline...");
app.UseOpenApi();
app.UseSwaggerUi();
app.MapControllers();
app.MapGet("/", () => "API is running!");

// Log when requests come in
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Incoming request: {Method} {Path}", context.Request.Method, context.Request.Path);
    await next();
    logger.LogInformation("Response: {StatusCode}", context.Response.StatusCode);
});

startupLogger.LogInformation("=== Application Configured Successfully ===");
startupLogger.LogInformation("Starting web host...");

app.Run();

// Helper method to mask password in connection string
static string MaskPassword(string connectionString)
{
    if (string.IsNullOrEmpty(connectionString)) return connectionString;

    var parts = connectionString.Split(';');
    for (int i = 0; i < parts.Length; i++)
    {
        if (parts[i].Contains("Password", StringComparison.OrdinalIgnoreCase))
        {
            var keyValue = parts[i].Split('=');
            if (keyValue.Length == 2)
            {
                parts[i] = $"{keyValue[0]}=***MASKED***";
            }
        }
    }
    return string.Join(";", parts);
}