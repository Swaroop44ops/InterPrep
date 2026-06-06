using Microsoft.EntityFrameworkCore;
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
var allowedOriginsSetting = builder.Configuration.GetValue<string>("CORS_ALLOWED_ORIGINS")
                             ?? Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")
                             ?? "http://localhost:5173,http://127.0.0.1:5173,http://localhost:5174,http://127.0.0.1:5174,http://localhost:5175,http://127.0.0.1:5175,http://localhost:3000";

var origins = allowedOriginsSetting.Split(',', StringSplitOptions.RemoveEmptyEntries);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(origins)
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

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

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

app.UseAuthorization();
app.MapControllers();

app.Run();
