using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudySessionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StudySessionsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/studysessions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudySession>>> GetStudySessions()
        {
            return await _context.StudySessions.OrderByDescending(s => s.CreatedAt).ToListAsync();
        }

        // POST: api/studysessions
        [HttpPost]
        public async Task<ActionResult<StudySession>> LogStudySession([FromBody] StudySession session)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            session.CreatedAt = DateTime.UtcNow;
            _context.StudySessions.Add(session);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStudySessions), new { id = session.Id }, session);
        }

        // GET: api/studysessions/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            // 1. Group historical sessions by date to build the heatmap data source
            var sessions = await _context.StudySessions.ToListAsync();
            var heatmap = sessions
                .GroupBy(s => s.CreatedAt.Date)
                .Select(g => new {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    count = g.Count(),
                    duration = g.Sum(s => s.DurationSeconds)
                })
                .ToList();

            // 2. Count confident questions per topic
            var confidentQuestions = await _context.Questions
                .Where(q => q.Status == "Confident")
                .GroupBy(q => q.TopicId)
                .Select(g => new {
                    topicId = g.Key,
                    count = g.Count()
                })
                .ToListAsync();

            // 3. Count due flashcards per topic
            var now = DateTime.UtcNow;
            var dueFlashcards = await _context.Flashcards
                .Where(f => f.NextReviewDate <= now)
                .GroupBy(f => f.TopicId)
                .Select(g => new {
                    topicId = g.Key,
                    count = g.Count()
                })
                .ToListAsync();

            return Ok(new {
                heatmap = heatmap,
                confidentQuestions = confidentQuestions,
                dueFlashcards = dueFlashcards
            });
        }
    }
}
