using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using backend.Data;
using backend.Models;
using backend.Repositories.Interfaces;
using backend.Repositories.Implementations;
using backend.Services.Interfaces;
using backend.Services.Implementations;
using backend.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

// 2. Configure CORS (set CORS_ALLOWED_ORIGINS on Render, e.g. https://inter-prep-tau.vercel.app)
var corsOriginsConfig = builder.Configuration["CORS_ALLOWED_ORIGINS"]
    ?? Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials();

        if (string.IsNullOrWhiteSpace(corsOriginsConfig))
        {
            policy.SetIsOriginAllowed(_ => true);
        }
        else
        {
            var origins = corsOriginsConfig.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            policy.WithOrigins(origins);
        }
    });
});

// Register clean architecture dependencies
// 1. Repositories
builder.Services.AddScoped<ITopicRepository, TopicRepository>();
builder.Services.AddScoped<INoteRepository, NoteRepository>();
builder.Services.AddScoped<IFlashcardRepository, FlashcardRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IStudySessionRepository, StudySessionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// 2. Services
builder.Services.AddScoped<ITopicService, TopicService>();
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<IFlashcardService, FlashcardService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IStudySessionService, StudySessionService>();
builder.Services.AddScoped<IUserService, UserService>();

// Add standard API services
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

// Configure JWT Authentication
var jwtSecretKey = JwtHelper.GetSecretKey();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = "TopicPortalHub",
        ValidateAudience = true,
        ValidAudience = "TopicPortalClients",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
    };
});

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

        await EnsureUsersSchemaAsync(context, services.GetRequiredService<ILogger<Program>>());

        // Ensure all default Topics are seeded in the database
        try
        {
            if (context.Database.IsSqlite())
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO ""Topics"" (""Id"", ""Title"", ""Description"", ""CreatedAt"")
                    VALUES 
                    (1, 'Getting Started with React', 'Learn the basics of React, including components, props, state, and hooks.', '2026-06-01 12:00:00'),
                    (2, 'Introduction to ASP.NET Core', 'Build secure, high-performance web APIs using .NET 9 and C#.', '2026-06-02 12:00:00'),
                    (3, 'Mastering EF Core', 'Connect your .NET applications to databases and write efficient LINQ queries.', '2026-06-03 12:00:00'),
                    (4, 'Supabase as a Backend', 'Utilize Supabase for authentication, real-time database, and storage solutions.', '2026-06-04 12:00:00')
                    ON CONFLICT (""Id"") DO NOTHING;
                ");
            }
            else
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO ""Topics"" (""Id"", ""Title"", ""Description"", ""CreatedAt"")
                    OVERRIDING SYSTEM VALUE
                    VALUES 
                    (1, 'Getting Started with React', 'Learn the basics of React, including components, props, state, and hooks.', '2026-06-01 12:00:00+00'),
                    (2, 'Introduction to ASP.NET Core', 'Build secure, high-performance web APIs using .NET 9 and C#.', '2026-06-02 12:00:00+00'),
                    (3, 'Mastering EF Core', 'Connect your .NET applications to databases and write efficient LINQ queries.', '2026-06-03 12:00:00+00'),
                    (4, 'Supabase as a Backend', 'Utilize Supabase for authentication, real-time database, and storage solutions.', '2026-06-04 12:00:00+00')
                    ON CONFLICT (""Id"") DO NOTHING;
                ");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Topic seeding warning: {ex.Message}");
        }

        // Programmatically seed PDF questions and flashcards if not already added
        var hasNewQuestions = await context.Questions.AnyAsync(q => q.Text.Contains("Single Responsibility Principle"));
        if (!hasNewQuestions)
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
                new Question { Text = "What is Hydration in Next.js?", Answer = "Hydration is the process where React renders the static HTML downloaded from the server on the client side, downloads the JavaScript bundle, and attaches event listeners to the HTML elements to make them interactive.", TopicId = 1, Difficulty = "Hard", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                
                // New PDF Questions
                new Question { Text = "What is the Single Responsibility Principle (SRP) and how is it implemented in C#?", Answer = "The Single Responsibility Principle states that a class should have only one reason to change, meaning it should perform only one job. For example, instead of having an Invoice class calculate prices and also print the invoice, we split them into InvoiceService (which calculates) and InvoicePrinter (which prints).", TopicId = 2, Difficulty = "Easy", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "What is the Open/Closed Principle (OCP) and how is it implemented in C#?", Answer = "The Open/Closed Principle states that software entities (classes, modules, functions) should be open for extension but closed for modification. In C#, this is typically achieved using interfaces or abstract classes. For example, instead of editing an existing discount calculation class to add a new discount type, we create a new class that implements an IDiscountStrategy interface.", TopicId = 2, Difficulty = "Medium", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "What is the Liskov Substitution Principle (LSP) and how is it implemented in C#?", Answer = "The Liskov Substitution Principle states that objects of a superclass should be replaceable with objects of its subclasses without breaking the application. A classic violation is making a Square class inherit from a Rectangle class, because changing the width of a Square also changes its height, breaking the area semantics of the Rectangle. In C#, avoid inheritance when subclasses change the expected behavior of base members.", TopicId = 2, Difficulty = "Medium", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "What is the Interface Segregation Principle (ISP) and how is it implemented in C#?", Answer = "The Interface Segregation Principle states that clients should not be forced to depend on interfaces they do not use. It is better to have many small, specific interfaces than one large, general-purpose one. For example, instead of a single ICRUD interface, we can split it into ISave and IDelete interfaces so that a read-only service is not forced to implement a delete method.", TopicId = 2, Difficulty = "Easy", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "What is the Dependency Inversion Principle (DIP) and how is it implemented in C#?", Answer = "The Dependency Inversion Principle states that high-level modules should not depend on low-level modules; both should depend on abstractions. Also, abstractions should not depend on details; details should depend on abstractions. In C#, this is implemented by using constructor injection to inject interfaces (e.g., IEmailService) instead of instantiating concrete classes directly, allowing us to swap implementations (e.g., SMTP vs SendGrid) without changing business logic.", TopicId = 2, Difficulty = "Medium", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "Explain the Singleton, Repository, Strategy, and Observer design patterns with their use cases.", Answer = "1. Singleton (Creational): Ensures a class has only one instance and provides a global point of access to it (e.g., a shared configuration/logger). 2. Repository (Structural): Abstracts data access behind interfaces, decoupling business logic from database layers. 3. Strategy (Behavioral): Defines a family of algorithms, encapsulates each one, and makes them interchangeable at runtime (e.g., swapping payment methods). 4. Observer (Behavioral): Defines a one-to-many dependency so that when one object changes state, all its dependents are notified automatically (e.g., event-driven notification systems).", TopicId = 2, Difficulty = "Medium", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "What is CQRS and the Mediator pattern, and how do they work together in .NET?", Answer = "CQRS (Command Query Responsibility Segregation) separates read operations (Queries) from write operations (Commands) into different models. The Mediator pattern decouples components by sending requests through a central mediator object. In .NET, libraries like MediatR are used to route Commands and Queries to their respective handlers, removing direct dependencies between controllers and business logic.", TopicId = 2, Difficulty = "Hard", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "What is Database Sharding, when should you use it, and what are its tradeoffs?", Answer = "Database Sharding is a horizontal scaling pattern where data is partitioned across multiple database instances (shards) based on a shard key. It is used when a single database becomes a bottleneck for storage or QPS. Tradeoffs: Increased application complexity, difficult cross-shard joins, complex transaction management (distributed transactions), and challenges in rebalancing shards.", TopicId = 4, Difficulty = "Hard", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "What is a Message Queue (like RabbitMQ or Kafka) and when should you use it?", Answer = "A Message Queue is an asynchronous communication mechanism used to decouple services, enable background processing, and handle traffic spikes (rate leveling). You should use it when a service does not need an immediate response to complete a task (e.g., sending email notifications, generating PDF reports, processing orders). Popular systems include RabbitMQ (message broker) and Apache Kafka (event streaming platform).", TopicId = 4, Difficulty = "Medium", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "What are SQL Window Functions and how do they differ from GROUP BY?", Answer = "Window functions perform calculations across a set of table rows that are related to the current row, but unlike GROUP BY, they do not collapse rows into a single output row. They use the OVER() clause (optionally with PARTITION BY and ORDER BY). Examples include ROW_NUMBER() (sequential numbers), RANK() (with gaps on ties), DENSE_RANK() (without gaps), and LAG()/LEAD() to access adjacent rows.", TopicId = 3, Difficulty = "Hard", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "What are the common types of database indexes (B-Tree, Hash, Partial, Composite) and how do they affect query performance?", Answer = "1. B-Tree index (default): Good for equality and range queries. 2. Hash index: Fastest for equality (=) checks, but does not support range queries. 3. Partial index: Indexes only a subset of rows matching a WHERE clause (e.g., active users only), saving space. 4. Composite index: Indexes multiple columns (order matters; query must filter by the leftmost prefix to use the index).", TopicId = 3, Difficulty = "Medium", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "Explain the four standard SQL transaction isolation levels and the anomalies they prevent.", Answer = "1. Read Uncommitted: Allows dirty reads (reading uncommitted changes), non-repeatable reads, and phantom reads. 2. Read Committed (PostgreSQL default): Prevents dirty reads. 3. Repeatable Read: Prevents dirty reads and non-repeatable reads (re-reading data returns the same values). 4. Serializable: Prevents all anomalies including phantom reads (transactions appear to execute sequentially), but has the lowest concurrency and high deadlock risk.", TopicId = 3, Difficulty = "Hard", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "What are the common web security threats (SQL Injection, XSS, CSRF, IDOR) and how do you mitigate them in ASP.NET Core?", Answer = "1. SQL Injection: Mitigated by using parameterized queries or ORMs like EF Core. 2. XSS (Cross-Site Scripting): Mitigated by encoding HTML output, enforcing Content Security Policy (CSP), and using HttpOnly flags on sensitive cookies. 3. CSRF (Cross-Site Request Forgery): Mitigated by using Anti-forgery tokens or SameSite cookie attribute. 4. IDOR (Insecure Direct Object Reference): Mitigated by verifying user ownership of resource IDs on the server side for every request.", TopicId = 2, Difficulty = "Medium", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "Compare the time and space complexity of Merge Sort and Quick Sort. When would you prefer one over the other?", Answer = "Merge Sort has a guaranteed time complexity of O(n log n) in all cases (best, average, worst) but requires O(n) auxiliary space and is stable. Quick Sort has an average time complexity of O(n log n) and O(log n) space but can degrade to O(n^2) in the worst case if pivot selection is poor, and is unstable. Prefer Merge Sort for stable sorting and linked lists, and Quick Sort for in-memory arrays when space is constrained.", TopicId = 3, Difficulty = "Hard", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "Explain the Sliding Window and Two Pointers algorithm patterns with examples.", Answer = "1. Two Pointers: Uses two index pointers to traverse a data structure (e.g., one pointer at each end of a sorted array moving toward each other to find a pair summing to target). 2. Sliding Window: Tracks a sub-range (window) of elements in an array or string that grows or shrinks dynamically (e.g., finding the longest substring without repeating characters, or the maximum sum subarray of size K).", TopicId = 3, Difficulty = "Medium", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "What is a deadlock in asynchronous C# code, and how do you avoid it?", Answer = "A deadlock occurs in async code when blocking sync calls like .Result or .Wait() are called on a Task within a synchronization context (e.g., ASP.NET legacy or UI applications). The thread blocks waiting for the task, while the task needs the blocked thread to complete its continuation. Mitigations: 1. Use async/await all the way down. 2. Use ConfigureAwait(false) in library code to allow continuations on any available thread pool thread.", TopicId = 2, Difficulty = "Medium", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "How would you design a secure and scalable file upload system?", Answer = "1. Scalability: Direct upload from client to Object Storage (like Amazon S3) using secure pre-signed URLs from the API server. This keeps file payloads off the API servers. 2. Security: Verify file signature/mime-type, limit file size, run malware scans (via background worker queue), and store metadata in a database. 3. Delivery: Serve files via a CDN (like CloudFront) using signed cookies or URLs for authorization.", TopicId = 4, Difficulty = "Hard", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "Explain the differences between Use(), Run(), and Map() methods in ASP.NET Core Middleware.", Answer = "1. Use(): Adds a middleware component to the pipeline. Can execute code before and after the next component using 'await next()'. 2. Run(): Terminal middleware. Does not call next, ending pipeline execution. 3. Map(): Branches the request pipeline dynamically based on matches with the request URL path.", TopicId = 2, Difficulty = "Medium", Status = "Unseen", CreatedAt = DateTime.UtcNow },
                new Question { Text = "Explain the differences between Next.js 14 App Router and Pages Router.", Answer = "1. Directory Structure: App Router uses app/ directory with folder-based routing and layout.tsx, page.tsx conventions. Pages Router uses pages/ directory. 2. React Server Components (RSC): App Router uses RSC by default (components render on server unless marked with 'use client'). Pages Router uses getServerSideProps/getStaticProps. 3. Data Fetching: App Router uses native async/await in Server Components and standard fetch(). Pages Router uses custom lifecycles.", TopicId = 1, Difficulty = "Medium", Status = "Unseen", CreatedAt = DateTime.UtcNow }
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
                new Flashcard { Front = "What is the CAP Theorem?", Back = "A distributed system can only guarantee at most two of: Consistency, Availability, and Partition Tolerance.", TopicId = 4, NextReviewDate = DateTime.UtcNow, IntervalDays = 1, CreatedAt = DateTime.UtcNow },
                
                // New PDF Flashcards
                new Flashcard { Front = "What is Database Sharding?", Back = "Horizontal partitioning of database rows across multiple instances using a shard key.", TopicId = 4, NextReviewDate = DateTime.UtcNow, IntervalDays = 1, CreatedAt = DateTime.UtcNow },
                new Flashcard { Front = "What are SQL Window Functions?", Back = "Calculations across related rows without collapsing them, using the OVER() clause.", TopicId = 3, NextReviewDate = DateTime.UtcNow, IntervalDays = 1, CreatedAt = DateTime.UtcNow },
                new Flashcard { Front = "What is a Message Queue?", Back = "Asynchronous communication channel (like RabbitMQ/Kafka) for decoupling services.", TopicId = 4, NextReviewDate = DateTime.UtcNow, IntervalDays = 1, CreatedAt = DateTime.UtcNow },
                new Flashcard { Front = "What is JSON vs JSONB in Postgres?", Back = "JSON stores plain text representation; JSONB stores parsed binary format which is indexable and faster.", TopicId = 3, NextReviewDate = DateTime.UtcNow, IntervalDays = 1, CreatedAt = DateTime.UtcNow },
                new Flashcard { Front = "What is a Deadlock in Async Code?", Back = "Two or more operations blocking each other, often caused by calling .Result or .Wait() in a sync context.", TopicId = 2, NextReviewDate = DateTime.UtcNow, IntervalDays = 1, CreatedAt = DateTime.UtcNow },
                new Flashcard { Front = "What is CSRF?", Back = "Cross-Site Request Forgery: Unauthorized commands submitted from a trusted user web browser.", TopicId = 2, NextReviewDate = DateTime.UtcNow, IntervalDays = 1, CreatedAt = DateTime.UtcNow },
                new Flashcard { Front = "What is XSS?", Back = "Cross-Site Scripting: Injecting malicious scripts into benign and trusted websites.", TopicId = 2, NextReviewDate = DateTime.UtcNow, IntervalDays = 1, CreatedAt = DateTime.UtcNow },
                new Flashcard { Front = "What is IDOR?", Back = "Insecure Direct Object Reference: Accessing database records by guessing/modifying identifier keys without auth check.", TopicId = 2, NextReviewDate = DateTime.UtcNow, IntervalDays = 1, CreatedAt = DateTime.UtcNow }
            };

            pdfQuestions.ForEach(q => q.UserId = 1);
            pdfFlashcards.ForEach(f => f.UserId = 1);

            // Add questions idempotently
            foreach (var q in pdfQuestions)
            {
                var exists = await context.Questions.AnyAsync(dbQ => dbQ.Text == q.Text);
                if (!exists)
                {
                    context.Questions.Add(q);
                }
            }

            // Add flashcards idempotently
            foreach (var f in pdfFlashcards)
            {
                var exists = await context.Flashcards.AnyAsync(dbF => dbF.Front == f.Front);
                if (!exists)
                {
                    context.Flashcards.Add(f);
                }
            }

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
var enableSwagger = app.Environment.IsDevelopment()
    || string.Equals(Environment.GetEnvironmentVariable("ENABLE_SWAGGER"), "true", StringComparison.OrdinalIgnoreCase);

if (enableSwagger)
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

var allowedOrigins = string.IsNullOrWhiteSpace(corsOriginsConfig)
    ? null
    : corsOriginsConfig.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

app.UseCors();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static async Task EnsureUsersSchemaAsync(AppDbContext context, ILogger logger)
{
    if (context.Database.IsSqlite())
    {
        await context.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""Users"" (
                ""Id"" INTEGER PRIMARY KEY AUTOINCREMENT,
                ""Username"" TEXT NOT NULL,
                ""PasswordHash"" TEXT NOT NULL,
                ""CreatedAt"" TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Username"" ON ""Users"" (""Username"");
        ");

        var demoPasswordHash = EncryptionHelper.Encrypt("password123");
        var demoCreatedAt = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        await context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO ""Users"" (""Id"", ""Username"", ""PasswordHash"", ""CreatedAt"")
            VALUES (1, 'demo', {demoPasswordHash}, {demoCreatedAt})
            ON CONFLICT (""Id"") DO NOTHING;
        ");
    }
    else
    {
        await context.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""Users"" (
                ""Id"" INTEGER GENERATED BY DEFAULT AS IDENTITY,
                ""Username"" text NOT NULL,
                ""PasswordHash"" text NOT NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                CONSTRAINT ""PK_Users"" PRIMARY KEY (""Id"")
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Username"" ON ""Users"" (""Username"");
        ");

        var demoPasswordHash = EncryptionHelper.Encrypt("password123");
        var demoCreatedAt = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        await context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO ""Users"" (""Id"", ""Username"", ""PasswordHash"", ""CreatedAt"")
            OVERRIDING SYSTEM VALUE
            VALUES (1, 'demo', {demoPasswordHash}, {demoCreatedAt})
            ON CONFLICT (""Id"") DO NOTHING;
        ");
    }

    var userColumnMigrations = new[]
    {
        @"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""RefreshToken"" text",
        @"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""RefreshTokenExpiry"" timestamp with time zone",
        @"ALTER TABLE ""Users"" ADD COLUMN ""RefreshToken"" TEXT",
        @"ALTER TABLE ""Users"" ADD COLUMN ""RefreshTokenExpiry"" DATETIME",
        @"ALTER TABLE ""Notes"" ADD COLUMN IF NOT EXISTS ""UserId"" INTEGER NOT NULL DEFAULT 1",
        @"ALTER TABLE ""Notes"" ADD COLUMN IF NOT EXISTS ""IsPublic"" BOOLEAN NOT NULL DEFAULT FALSE",
        @"UPDATE ""Notes"" SET ""IsPublic"" = TRUE WHERE ""UserId"" = 1",
        @"ALTER TABLE ""Flashcards"" ADD COLUMN IF NOT EXISTS ""UserId"" INTEGER NOT NULL DEFAULT 1",
        @"ALTER TABLE ""Questions"" ADD COLUMN IF NOT EXISTS ""UserId"" INTEGER NOT NULL DEFAULT 1",
        @"ALTER TABLE ""StudySessions"" ADD COLUMN IF NOT EXISTS ""UserId"" INTEGER NOT NULL DEFAULT 1",
        @"ALTER TABLE ""Questions"" DROP COLUMN IF EXISTS ""Status""",
        @"ALTER TABLE ""Questions"" DROP COLUMN ""Status""",
    };

    foreach (var sql in userColumnMigrations)
    {
        try
        {
            await context.Database.ExecuteSqlRawAsync(sql);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "User column migration skipped.");
        }
    }

    var foreignKeyMigrations = new[]
    {
        @"ALTER TABLE ""Notes"" DROP CONSTRAINT IF EXISTS ""FK_Notes_Users_UserId""",
        @"ALTER TABLE ""Notes"" ADD CONSTRAINT ""FK_Notes_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE",
        @"ALTER TABLE ""Flashcards"" DROP CONSTRAINT IF EXISTS ""FK_Flashcards_Users_UserId""",
        @"ALTER TABLE ""Flashcards"" ADD CONSTRAINT ""FK_Flashcards_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE",
        @"ALTER TABLE ""Questions"" DROP CONSTRAINT IF EXISTS ""FK_Questions_Users_UserId""",
        @"ALTER TABLE ""Questions"" ADD CONSTRAINT ""FK_Questions_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE",
        @"ALTER TABLE ""StudySessions"" DROP CONSTRAINT IF EXISTS ""FK_StudySessions_Users_UserId""",
        @"ALTER TABLE ""StudySessions"" ADD CONSTRAINT ""FK_StudySessions_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE",
    };

    foreach (var sql in foreignKeyMigrations)
    {
        try
        {
            await context.Database.ExecuteSqlRawAsync(sql);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "User foreign key migration skipped.");
        }
    }

    // Create UserQuestionStatuses table if it doesn't exist
    try
    {
        if (context.Database.IsSqlite())
        {
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""UserQuestionStatuses"" (
                    ""Id"" INTEGER PRIMARY KEY AUTOINCREMENT,
                    ""UserId"" INTEGER NOT NULL,
                    ""QuestionId"" INTEGER NOT NULL,
                    ""Status"" TEXT NOT NULL,
                    ""UpdatedAt"" TEXT NOT NULL,
                    FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE,
                    FOREIGN KEY (""QuestionId"") REFERENCES ""Questions"" (""Id"") ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_UserQuestionStatuses_UserId_QuestionId"" ON ""UserQuestionStatuses"" (""UserId"", ""QuestionId"");
            ");
        }
        else
        {
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""UserQuestionStatuses"" (
                    ""Id"" INTEGER GENERATED BY DEFAULT AS IDENTITY,
                    ""UserId"" integer NOT NULL,
                    ""QuestionId"" integer NOT NULL,
                    ""Status"" text NOT NULL,
                    ""UpdatedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_UserQuestionStatuses"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_UserQuestionStatuses_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_UserQuestionStatuses_Questions_QuestionId"" FOREIGN KEY (""QuestionId"") REFERENCES ""Questions"" (""Id"") ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_UserQuestionStatuses_UserId_QuestionId"" ON ""UserQuestionStatuses"" (""UserId"", ""QuestionId"");
            ");
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "UserQuestionStatuses table migration failed.");
    }

    await MigrateQuestionsSchemaAsync(context, logger);

    logger.LogInformation("Users schema ensured.");
}

static async Task MigrateQuestionsSchemaAsync(AppDbContext context, ILogger logger)
{
    try
    {
        // 1. Fetch cloned questions (where UserId != 1)
        var clonedQuestions = await context.Questions
            .Where(q => q.UserId != 1)
            .ToListAsync();

        if (clonedQuestions.Count == 0)
        {
            return; // No cloned questions to migrate
        }

        logger.LogInformation("Found {Count} cloned questions. Migrating user statuses...", clonedQuestions.Count);

        var adminQuestions = await context.Questions
            .Where(q => q.UserId == 1)
            .ToListAsync();

        var migratedCount = 0;
        var toDelete = new List<Question>();

        foreach (var userQuestion in clonedQuestions)
        {
            // Find corresponding admin question by TopicId and Text match
            var adminQuestion = adminQuestions.FirstOrDefault(aq => 
                aq.TopicId == userQuestion.TopicId && 
                string.Equals(aq.Text.Trim(), userQuestion.Text.Trim(), StringComparison.OrdinalIgnoreCase));

            if (adminQuestion != null)
            {
                // Verify if a status record already exists
                var exists = await context.UserQuestionStatuses.AnyAsync(us => 
                    us.UserId == userQuestion.UserId && 
                    us.QuestionId == adminQuestion.Id);

                if (!exists)
                {
                    var userStatus = new UserQuestionStatus
                    {
                        UserId = userQuestion.UserId,
                        QuestionId = adminQuestion.Id,
                        Status = userQuestion.Status,
                        UpdatedAt = DateTime.UtcNow
                    };
                    context.UserQuestionStatuses.Add(userStatus);
                    migratedCount++;
                }

                // Mark cloned question for deletion
                toDelete.Add(userQuestion);
            }
        }

        if (toDelete.Count > 0)
        {
            context.Questions.RemoveRange(toDelete);
            await context.SaveChangesAsync();
            logger.LogInformation("Successfully migrated {MigratedCount} question statuses and cleaned up {DeletedCount} duplicate questions.", migratedCount, toDelete.Count);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to run questions schema migration.");
    }
}
