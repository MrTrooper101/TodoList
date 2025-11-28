using Microsoft.EntityFrameworkCore;
using TodoApp.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Read connection string from environment or appsettings
var connectionString = Environment.GetEnvironmentVariable("DefaultConnection")
                       ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();

// NSwag/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "Todo API";
    config.Version = "v1";
});

var app = builder.Build();

// Attempt to connect to database and log success/failure
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        logger.LogInformation("Attempting to connect to database...");
        db.Database.OpenConnection(); // test connection
        logger.LogInformation("Database connection successful.");

        // Optional: apply migrations
        db.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database connection failed!");
        throw; // stop app if DB connection fails
    }
}

// Configure middleware
app.UseOpenApi();
app.UseSwaggerUi();

app.MapControllers();
app.MapGet("/", () => "API is running!");

// Listen on Railway port
var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";
app.Urls.Add($"http://*:{port}");

app.Run();
