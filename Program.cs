using Microsoft.EntityFrameworkCore;
using TodoApp.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Register SQLite
//builder.Services.AddDbContext<AppDbContext>(options =>
//    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddControllers();

// Add NSwag OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "Todo API";
    config.Version = "v1";
});
builder.Services.AddControllers();
var app = builder.Build();

// Configure NSwag UI
app.UseOpenApi();
app.UseSwaggerUi();

app.MapControllers();
app.MapGet("/", () => "API is running!");

app.Run();