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
    public class FlashcardsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FlashcardsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/flashcards?topicId=1
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Flashcard>>> GetFlashcards([FromQuery] int? topicId)
        {
            await _context.Database.EnsureCreatedAsync();

            IQueryable<Flashcard> query = _context.Flashcards;
            if (topicId.HasValue && topicId.Value > 0)
            {
                query = query.Where(f => f.TopicId == topicId.Value);
            }

            return await query.ToListAsync();
        }

        // POST: api/flashcards
        [HttpPost]
        public async Task<ActionResult<Flashcard>> CreateFlashcard([FromBody] Flashcard flashcard)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var topicExists = await _context.Topics.AnyAsync(t => t.Id == flashcard.TopicId);
            if (!topicExists)
            {
                return BadRequest("Invalid Topic ID.");
            }

            flashcard.NextReviewDate = DateTime.UtcNow; // Default due immediately
            flashcard.IntervalDays = 1;
            flashcard.CreatedAt = DateTime.UtcNow;

            _context.Flashcards.Add(flashcard);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFlashcardById), new { id = flashcard.Id }, flashcard);
        }

        // GET: api/flashcards/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Flashcard>> GetFlashcardById(int id)
        {
            var card = await _context.Flashcards.FindAsync(id);
            if (card == null) return NotFound();
            return card;
        }

        // POST: api/flashcards/5/review?quality=easy
        [HttpPost("{id}/review")]
        public async Task<ActionResult<Flashcard>> ReviewFlashcard(int id, [FromQuery] string quality)
        {
            var card = await _context.Flashcards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            if (string.Equals(quality, "easy", StringComparison.OrdinalIgnoreCase))
            {
                // Double the interval and push the review date out
                card.IntervalDays = Math.Max(1, card.IntervalDays * 2);
                card.NextReviewDate = DateTime.UtcNow.AddDays(card.IntervalDays);
            }
            else // "hard" or default
            {
                // Reset interval to 1 day
                card.IntervalDays = 1;
                card.NextReviewDate = DateTime.UtcNow.AddDays(1);
            }

            await _context.SaveChangesAsync();
            return Ok(card);
        }

        // DELETE: api/flashcards/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFlashcard(int id)
        {
            var card = await _context.Flashcards.FindAsync(id);
            if (card == null) return NotFound();

            _context.Flashcards.Remove(card);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
