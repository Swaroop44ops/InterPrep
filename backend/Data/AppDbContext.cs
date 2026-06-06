using Microsoft.EntityFrameworkCore;
using backend.Models;
using System;

namespace backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Topic> Topics { get; set; } = null!;
        public DbSet<Note> Notes { get; set; } = null!;
        public DbSet<Flashcard> Flashcards { get; set; } = null!;
        public DbSet<Question> Questions { get; set; } = null!;
        public DbSet<StudySession> StudySessions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Cascade Delete configuration for Notes
            modelBuilder.Entity<Note>()
                .HasOne(n => n.Topic)
                .WithMany()
                .HasForeignKey(n => n.TopicId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cascade Delete configuration for Flashcards
            modelBuilder.Entity<Flashcard>()
                .HasOne(f => f.Topic)
                .WithMany()
                .HasForeignKey(f => f.TopicId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cascade Delete configuration for Questions
            modelBuilder.Entity<Question>()
                .HasOne(q => q.Topic)
                .WithMany()
                .HasForeignKey(q => q.TopicId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // 1. Seed Topics
            modelBuilder.Entity<Topic>().HasData(
                new Topic 
                { 
                    Id = 1, 
                    Title = "Getting Started with React", 
                    Description = "Learn the basics of React, including components, props, state, and hooks.", 
                    CreatedAt = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc) 
                },
                new Topic 
                { 
                    Id = 2, 
                    Title = "Introduction to ASP.NET Core", 
                    Description = "Build secure, high-performance web APIs using .NET 9 and C#.", 
                    CreatedAt = new DateTime(2026, 6, 2, 12, 0, 0, DateTimeKind.Utc) 
                },
                new Topic 
                { 
                    Id = 3, 
                    Title = "Mastering EF Core", 
                    Description = "Connect your .NET applications to databases and write efficient LINQ queries.", 
                    CreatedAt = new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc) 
                },
                new Topic 
                { 
                    Id = 4, 
                    Title = "Supabase as a Backend", 
                    Description = "Utilize Supabase for authentication, real-time database, and storage solutions.", 
                    CreatedAt = new DateTime(2026, 6, 4, 12, 0, 0, DateTimeKind.Utc) 
                }
            );

            // 2. Seed Notes
            modelBuilder.Entity<Note>().HasData(
                new Note
                {
                    Id = 1,
                    Title = "Understanding React Components",
                    Content = "<h2>React Components</h2><p>Components are the building blocks of React applications. They let you split the UI into independent, reusable pieces.</p><p>Here is a simple functional component:</p><pre><code>function Welcome(props) {\n  return &lt;h1&gt;Hello, {props.name}&lt;/h1&gt;;\n}</code></pre><p>They can receive inputs called <strong>props</strong> and manage local state with the <strong>useState</strong> hook.</p>",
                    TopicId = 1,
                    CreatedAt = new DateTime(2026, 6, 1, 14, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 6, 1, 14, 30, 0, DateTimeKind.Utc)
                },
                new Note
                {
                    Id = 2,
                    Title = "React Hooks Overview",
                    Content = "<h2>React Hooks</h2><p>Hooks let you use state and other React features without writing a class. Some common hooks are:</p><ul><li><strong>useState</strong>: Manages local component state.</li><li><strong>useEffect</strong>: Performs side effects (data fetching, subscriptions).</li><li><strong>useContext</strong>: Subscribes to React context.</li></ul>",
                    TopicId = 1,
                    CreatedAt = new DateTime(2026, 6, 2, 10, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 6, 2, 10, 0, 0, DateTimeKind.Utc)
                },
                new Note
                {
                    Id = 3,
                    Title = "Controllers in ASP.NET Core",
                    Content = "<h2>API Controllers</h2><p>In ASP.NET Core, controllers handle incoming HTTP requests and return HTTP responses. You annotate them with <code>[ApiController]</code>.</p><pre><code>[ApiController]\n[Route(\"api/[controller]\")]\npublic class ProductsController : ControllerBase {\n    [HttpGet]\n    public IActionResult GetAll() => Ok(new string[] { \"A\", \"B\" });\n}</code></pre>",
                    TopicId = 2,
                    CreatedAt = new DateTime(2026, 6, 3, 9, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 6, 3, 9, 15, 0, DateTimeKind.Utc)
                },
                new Note
                {
                    Id = 4,
                    Title = "EF Core Configurations",
                    Content = "<h2>Database Mapping</h2><p>Entity Framework Core allows mapping C# classes to database tables. You can customize properties using Fluent API in the <code>OnModelCreating</code> method of your DbContext:</p><pre><code>modelBuilder.Entity&lt;Note&gt;()\n    .Property(n =&gt; n.Title)\n    .IsRequired();</code></pre>",
                    TopicId = 3,
                    CreatedAt = new DateTime(2026, 6, 4, 11, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 6, 4, 11, 0, 0, DateTimeKind.Utc)
                }
            );

            // 3. Seed Flashcards
            modelBuilder.Entity<Flashcard>().HasData(
                new Flashcard
                {
                    Id = 1,
                    Front = "What is JSX in React?",
                    Back = "JSX is a syntax extension for JavaScript. It allows writing HTML-like code inside JavaScript files that React translates into elements.",
                    TopicId = 1,
                    NextReviewDate = DateTime.UtcNow, // Due today
                    IntervalDays = 1,
                    CreatedAt = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc)
                },
                new Flashcard
                {
                    Id = 2,
                    Front = "What are the rules of Hooks?",
                    Back = "1. Only call Hooks at the top level (not inside loops or conditions).\n2. Only call Hooks from React function components or custom Hooks.",
                    TopicId = 1,
                    NextReviewDate = DateTime.UtcNow, // Due today
                    IntervalDays = 1,
                    CreatedAt = new DateTime(2026, 6, 1, 10, 15, 0, DateTimeKind.Utc)
                },
                new Flashcard
                {
                    Id = 3,
                    Front = "What does [ApiController] do in ASP.NET Core?",
                    Back = "It provides features like automatic model validation responses (HTTP 400), automatic parameter binding from request bodies, and enforcement of attribute routing.",
                    TopicId = 2,
                    NextReviewDate = DateTime.UtcNow.AddDays(2), // Not due yet
                    IntervalDays = 2,
                    CreatedAt = new DateTime(2026, 6, 2, 10, 0, 0, DateTimeKind.Utc)
                },
                new Flashcard
                {
                    Id = 4,
                    Front = "Name the three tracking states of EF Core.",
                    Back = "1. Added (to be inserted)\n2. Modified (to be updated)\n3. Unchanged (loaded from database and not modified)\n4. Deleted (to be deleted)",
                    TopicId = 3,
                    NextReviewDate = DateTime.UtcNow, // Due today
                    IntervalDays = 1,
                    CreatedAt = new DateTime(2026, 6, 3, 10, 0, 0, DateTimeKind.Utc)
                }
            );

            // 4. Seed Questions
            modelBuilder.Entity<Question>().HasData(
                new Question
                {
                    Id = 1,
                    Text = "How do you pass data from a parent component to a child component in React?",
                    Answer = "By passing custom key-value pairs as 'props' (properties) in the JSX tag. Props are read-only and immutable within the child.",
                    TopicId = 1,
                    Difficulty = "Easy",
                    Status = "Confident",
                    CreatedAt = new DateTime(2026, 6, 1, 11, 0, 0, DateTimeKind.Utc)
                },
                new Question
                {
                    Id = 2,
                    Text = "What is the purpose of returning a function from the useEffect hook?",
                    Answer = "It serves as the cleanup function. React runs this cleanup function when the component unmounts and before running the effect again to prevent memory leaks.",
                    TopicId = 1,
                    Difficulty = "Medium",
                    Status = "Attempted",
                    CreatedAt = new DateTime(2026, 6, 1, 11, 30, 0, DateTimeKind.Utc)
                },
                new Question
                {
                    Id = 3,
                    Text = "Explain Dependency Injection (DI) lifetimes in ASP.NET Core.",
                    Answer = "Transient: Created each time requested.\nScoped: Created once per client request (connection).\nSingleton: Created once and shared application-wide.",
                    TopicId = 2,
                    Difficulty = "Medium",
                    Status = "Unseen",
                    CreatedAt = new DateTime(2026, 6, 2, 11, 0, 0, DateTimeKind.Utc)
                },
                new Question
                {
                    Id = 4,
                    Text = "What is the difference between client-side and server-side evaluation in EF Core?",
                    Answer = "Server-side evaluation translates LINQ queries to SQL. Client-side evaluation executes non-translatable parts in memory on the server after fetching base rows.",
                    TopicId = 3,
                    Difficulty = "Hard",
                    Status = "Unseen",
                    CreatedAt = new DateTime(2026, 6, 3, 11, 0, 0, DateTimeKind.Utc)
                }
            );

            // 5. Seed Historical Study Sessions for Heatmap (past week)
            modelBuilder.Entity<StudySession>().HasData(
                new StudySession { Id = 1, DurationSeconds = 600, NotesReviewedCount = 2, CardsReviewedCount = 4, QuestionsAttemptedCount = 1, CreatedAt = DateTime.UtcNow.AddDays(-7) },
                new StudySession { Id = 2, DurationSeconds = 1200, NotesReviewedCount = 4, CardsReviewedCount = 6, QuestionsAttemptedCount = 2, CreatedAt = DateTime.UtcNow.AddDays(-6) },
                new StudySession { Id = 3, DurationSeconds = 450, NotesReviewedCount = 1, CardsReviewedCount = 2, QuestionsAttemptedCount = 0, CreatedAt = DateTime.UtcNow.AddDays(-5) },
                // skip day -4 to simulate an idle day
                new StudySession { Id = 4, DurationSeconds = 1800, NotesReviewedCount = 6, CardsReviewedCount = 10, QuestionsAttemptedCount = 4, CreatedAt = DateTime.UtcNow.AddDays(-3) },
                new StudySession { Id = 5, DurationSeconds = 900, NotesReviewedCount = 3, CardsReviewedCount = 5, QuestionsAttemptedCount = 2, CreatedAt = DateTime.UtcNow.AddDays(-2) },
                new StudySession { Id = 6, DurationSeconds = 2400, NotesReviewedCount = 8, CardsReviewedCount = 12, QuestionsAttemptedCount = 5, CreatedAt = DateTime.UtcNow.AddDays(-1) }
            );
        }
    }
}
