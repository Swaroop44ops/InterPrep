using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FlashcardsController : ControllerBase
    {
        private readonly IFlashcardService _flashcardService;
        private readonly ITopicService _topicService;

        public FlashcardsController(IFlashcardService flashcardService, ITopicService topicService)
        {
            _flashcardService = flashcardService;
            _topicService = topicService;
        }

        // GET: api/flashcards?topicId=1
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Flashcard>>> GetFlashcards(
            [FromHeader(Name = "X-User-Id")] int? userId,
            [FromQuery] int? topicId)
        {
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized("Missing or invalid user context (X-User-Id header).");
            }

            var cards = await _flashcardService.GetFlashcardsAsync(topicId, userId.Value);
            return Ok(cards);
        }

        // POST: api/flashcards
        [HttpPost]
        public async Task<ActionResult<Flashcard>> CreateFlashcard(
            [FromHeader(Name = "X-User-Id")] int? userId,
            [FromBody] Flashcard flashcard)
        {
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized("Missing or invalid user context (X-User-Id header).");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var topicExists = await _topicService.TopicExistsAsync(flashcard.TopicId);
            if (!topicExists)
            {
                return BadRequest("Invalid Topic ID.");
            }

            var createdCard = await _flashcardService.CreateFlashcardAsync(flashcard, userId.Value);
            return CreatedAtAction(nameof(GetFlashcardById), new { id = createdCard.Id, userId = userId.Value }, createdCard);
        }

        // GET: api/flashcards/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Flashcard>> GetFlashcardById(
            [FromHeader(Name = "X-User-Id")] int? userId,
            int id)
        {
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized("Missing or invalid user context (X-User-Id header).");
            }

            var card = await _flashcardService.GetFlashcardByIdAsync(id, userId.Value);
            if (card == null) return NotFound();
            return Ok(card);
        }

        // POST: api/flashcards/5/review?quality=easy
        [HttpPost("{id}/review")]
        public async Task<ActionResult<Flashcard>> ReviewFlashcard(
            [FromHeader(Name = "X-User-Id")] int? userId,
            int id,
            [FromQuery] string quality)
        {
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized("Missing or invalid user context (X-User-Id header).");
            }

            var card = await _flashcardService.ReviewFlashcardAsync(id, quality, userId.Value);
            if (card == null) return NotFound();
            return Ok(card);
        }

        // DELETE: api/flashcards/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFlashcard(
            [FromHeader(Name = "X-User-Id")] int? userId,
            int id)
        {
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized("Missing or invalid user context (X-User-Id header).");
            }

            var deleted = await _flashcardService.DeleteFlashcardAsync(id, userId.Value);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
