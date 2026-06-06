using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using backend.Data;
using System;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Connection String (prefer Config, fallback to DATABASE_URL for Render/Supabase)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                      ?? Environment.GetEnvironmentVariable("DATABASE_URL")
                      ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    // Fallback to local SQLite for immediate preview and testing
    Console.WriteLine("Warning: No connection string found. Falling back to SQLite: Data Source=topic_hub.db");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite("Data Source=topic_hub.db"));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}

// 2. Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add standard API services
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Initialize Database Schema and Seed Data on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        if (context.Database.IsSqlite())
        {
            await context.Database.EnsureCreatedAsync();
        }
        else
        {
            var databaseCreator = context.Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;
            if (databaseCreator != null)
            {
                if (!databaseCreator.Exists())
                {
                    await databaseCreator.CreateAsync();
                }
                
                var connection = context.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }
                
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename IN ('Topics', 'topics'));";
                var result = (bool?)await command.ExecuteScalarAsync();
                var tableExists = result ?? false;

                if (!tableExists)
                {
                    Console.WriteLine("Topics table not found in Supabase. Creating tables...");
                    await databaseCreator.CreateTablesAsync();
                    Console.WriteLine("Database tables created and seeded successfully.");
                }
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// For Render deployment, bind to PORT environment variable if available
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    app.Urls.Add($"http://*:{port}");
}
else
{
    app.Urls.Add("http://localhost:5100");
}

app.UseCors("AllowAll");
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();
