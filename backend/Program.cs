using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using backend.Data;
using backend.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        // Programmatically seed PDF questions and flashcards if not already added
        var questionCount = await context.Questions.CountAsync();
        if (questionCount <= 4)
        {
            Console.WriteLine("Seeding PDF questions and flashcards into database...");
            
            var pdfQuestions = new List<Question>
            {
                new Question { Text = "What are Extension Methods in C#?", Answer = "Extension methods allow adding new methods to existing classes without modifying the original class. They are static methods inside a static class and use the 'this' keyword on the first parameter to specify the type they extend.", TopicId = 2, Difficulty = "Easy", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "What is Middleware in ASP.NET Core?", Answer = "Middleware are software components assembled into an application pipeline to handle requests and responses. Each component chooses whether to pass the request to the next component in the pipeline, and can perform work before and after the next component in the pipeline.", TopicId = 2, Difficulty = "Easy", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "What is an API Gateway?", Answer = "An API Gateway acts as a single entry point for multiple microservices, handling routing, authentication, rate limiting, load balancing, and logging. Popular examples include Ocelot, YARP, Kong, and NGINX.", TopicId = 2, Difficulty = "Medium", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "What is the difference between Filters and Middleware in ASP.NET Core?", Answer = "Middleware operates globally on the entire HTTP request pipeline before the request reaches the MVC/routing framework. Filters run only inside the MVC/Web API pipeline (after routing) and are specific to MVC actions/controllers.", TopicId = 2, Difficulty = "Medium", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "How do you sort an array and move all zeros to the end using LINQ?", Answer = "You can filter out the non-zero elements, sort them, and then concatenate them with the zero elements: arr.Where(x => x != 0).OrderBy(x => x).Concat(arr.Where(x => x == 0)).ToArray();", TopicId = 3, Difficulty = "Medium", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "What is Rate Limiting in ASP.NET Core?", Answer = "Rate limiting restricts the number of requests a client can make within a certain time window to prevent DDOS attacks, prevent API abuse, and improve resource performance. It can be configured using builder.Services.AddRateLimiter().", TopicId = 2, Difficulty = "Medium", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "What are some common LINQ optimization techniques?", Answer = "1. Use Any() instead of Count() > 0. 2. Use FirstOrDefault() carefully. 3. Avoid multiple enumerations. 4. Use Select() to retrieve only required columns. 5. Use AsNoTracking() in EF Core for read-only queries.", TopicId = 3, Difficulty = "Medium", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "Explain the difference between IEnumerable and IQueryable in C# / .NET.", Answer = "IEnumerable executes queries in-memory (LINQ to Objects) after pulling all data from the database. IQueryable translates queries to SQL and executes them inside the database server (LINQ to Entities), retrieving only the filtered records.", TopicId = 3, Difficulty = "Medium", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "What is Dependency Injection?", Answer = "Dependency Injection (DI) is a software design pattern used to achieve loose coupling between classes and their dependencies. Instead of creating dependencies directly, they are registered with a DI container and injected (e.g. via constructor injection).", TopicId = 2, Difficulty = "Easy", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "Explain async/await state machine in C#.", Answer = "async/await is syntactic sugar over Tasks. The compiler rewrites the method as a state machine. It allows asynchronous programming without blocking threads, yielding execution when waiting for task completion.", TopicId = 2, Difficulty = "Hard", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "What is JWT (JSON Web Token)?", Answer = "JWT is an open standard (RFC 7519) that defines a compact and self-contained way for securely transmitting information between parties as a JSON object. It is commonly used for stateless authentication and authorization.", TopicId = 2, Difficulty = "Medium", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "What is CORS (Cross-Origin Resource Sharing)?", Answer = "CORS is a browser security mechanism that uses HTTP headers to tell browsers to give a web application running at one origin access to selected resources from a different origin.", TopicId = 2, Difficulty = "Easy", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "What is the difference between Interface and Abstract Class?", Answer = "An interface is a pure contract with no state and supports multiple inheritance. An abstract class can have state, partial implementation, constructor, and supports only single inheritance.", TopicId = 2, Difficulty = "Easy", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "Explain Value Types vs Reference Types in C#.", Answer = "Value types (like int, struct, enum) are stored directly on the stack and copied on assignment. Reference types (like class, string, array) are stored on the heap, and assignment copies only the reference to that heap memory.", TopicId = 2, Difficulty = "Easy", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "What is boxing and unboxing in C#?", Answer = "Boxing is the process of converting a value type to the type object (which allocates it on the heap). Unboxing is casting it back to its original value type. Boxing causes GC pressure and should be avoided in performance-critical code paths.", TopicId = 2, Difficulty = "Medium", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "Explain the three Dependency Injection lifetimes in ASP.NET Core.", Answer = "1. Transient: A new instance is created every time it is requested. 2. Scoped: A single instance is created per HTTP request lifecycle. 3. Singleton: A single instance is created once on app startup and shared application-wide.", TopicId = 2, Difficulty = "Medium", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "How do you avoid the N+1 query problem in EF Core?", Answer = "By using Eager Loading via .Include() to fetch related data in a single SQL query, or by using Projections via .Select() to retrieve only the required fields instead of loading full entities.", TopicId = 3, Difficulty = "Hard", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "Explain the CAP Theorem in System Design.", Answer = "The CAP theorem states that a distributed data store can simultaneously provide at most two of three guarantees: Consistency (every read receives the most recent write or an error), Availability (every request receives a non-error response), and Partition tolerance (the system continues to operate despite network partition).", TopicId = 4, Difficulty = "Hard", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "What is the difference between useMemo and useCallback in React?", Answer = "useMemo memoizes the computed value of an expensive function so it doesn't recalculate unless dependencies change. useCallback memoizes the actual function reference itself to prevent child re-renders.", TopicId = 1, Difficulty = "Medium", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "What is Hydration in Next.js?", Answer = "Hydration is the process where React renders the static HTML downloaded from the server on the client side, downloads the JavaScript bundle, and attaches event listeners to the HTML elements to make them interactive.", TopicId = 1, Difficulty = "Hard", Status = "Unseen", CreatedAt = DateTime.UtcNow }
            };

            var pdfFlashcards = new List<Flashcard>
            {
                new Flashcard { Front = "What are C# Extension Methods?", Back = "Static methods inside a static class using the 'this' keyword to add functionality to existing types without modifying them.", TopicId = 2, NextReviewDate = DateTime.UtcNow, IntervalDays = 1, CreatedAt = DateTime.UtcNow },
                new Flashcard { Front = "What is Middleware in ASP.NET Core?", Back = "Software components in the HTTP request pipeline that process requests and responses sequentially.", TopicId = 2, NextReviewDate = DateTime.UtcNow, IntervalDays = 1, CreatedAt = DateTime.UtcNow },
                new Flashcard { Front = "IEnumerable vs IQueryable?", Back = "IEnumerable runs queries in-memory after fetching data. IQueryable translates queries to SQL and runs them directly in the database.", TopicId = 3, NextReviewDate = DateTime.UtcNow, IntervalDays = 1, CreatedAt = DateTime.UtcNow },
                new Flashcard { Front = "What are the OOP pillars?", Back = "Encapsulation (hide data), Inheritance (reuse code), Polymorphism (override/overload methods), and Abstraction (hide complexity).", TopicId = 2, NextReviewDate = DateTime.UtcNow, IntervalDays = 1, CreatedAt = DateTime.UtcNow },
                new Flashcard { Front = "What does the S in SOLID stand for?", Back = "Single Responsibility Principle: A class should have only one reason to change.", TopicId = 2, NextReviewDate = DateTime.UtcNow, IntervalDays = 1, CreatedAt = DateTime.UtcNow },
                new Flashcard { Front = "What does the O in SOLID stand for?", Back = "Open/Closed Principle: Software entities should be open for extension but closed for modification.", TopicId = 2, NextReviewDate = DateTime.UtcNow, IntervalDays = 1, CreatedAt = DateTime.UtcNow },
                new Flashcard { Front = "What does the L in SOLID stand for?", Back = "Liskov Substitution Principle: Derived classes must be substitutable for their base classes.", TopicId = 2, NextReviewDate = DateTime.UtcNow, IntervalDays = 1, CreatedAt = DateTime.UtcNow },
                new Flashcard { Front = "What does the I in SOLID stand for?", Back = "Interface Segregation Principle: Many client-specific interfaces are better than one general-purpose interface.", TopicId = 2, NextReviewDate = DateTime.UtcNow, IntervalDays = 1, CreatedAt = DateTime.UtcNow },
                new Flashcard { Front = "What does the D in SOLID stand for?", Back = "Dependency Inversion Principle: Depend on abstractions, not on concrete implementations.", TopicId = 2, NextReviewDate = DateTime.UtcNow, IntervalDays = 1, CreatedAt = DateTime.UtcNow },
                new Flashcard { Front = "What is useMemo vs useCallback?", Back = "useMemo caches the result of an expensive calculation. useCallback caches the function reference itself to prevent unnecessary re-creations.", TopicId = 1, NextReviewDate = DateTime.UtcNow, IntervalDays = 1, CreatedAt = DateTime.UtcNow },
                new Flashcard { Front = "How do you avoid N+1 query problem in EF Core?", Back = "Use Eager Loading (.Include()) to fetch related tables in one query, or use Projections (.Select()) to retrieve specific properties.", TopicId = 3, NextReviewDate = DateTime.UtcNow, IntervalDays = 1, CreatedAt = DateTime.UtcNow },
                new Flashcard { Front = "What are the DI lifetimes in ASP.NET Core?", Back = "Transient (created per request of service), Scoped (created per HTTP request), and Singleton (created once on app start).", TopicId = 2, NextReviewDate = DateTime.UtcNow, IntervalDays = 1, CreatedAt = DateTime.UtcNow },
                new Flashcard { Front = "What is the CAP Theorem?", Back = "A distributed system can only guarantee at most two of: Consistency, Availability, and Partition Tolerance.", TopicId = 4, NextReviewDate = DateTime.UtcNow, IntervalDays = 1, CreatedAt = DateTime.UtcNow }
            };

            context.Questions.AddRange(pdfQuestions);
            context.Flashcards.AddRange(pdfFlashcards);
            await context.SaveChangesAsync();
            Console.WriteLine("PDF questions and flashcards successfully seeded!");
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
