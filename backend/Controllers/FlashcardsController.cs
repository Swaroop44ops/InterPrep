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
        public async Task<ActionResult<IEnumerable<Flashcard>>> GetFlashcards([FromQuery] int? topicId)
        {
            var cards = await _flashcardService.GetFlashcardsAsync(topicId);
            return Ok(cards);
        }

        // POST: api/flashcards
        [HttpPost]
        public async Task<ActionResult<Flashcard>> CreateFlashcard([FromBody] Flashcard flashcard)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var topicExists = await _topicService.TopicExistsAsync(flashcard.TopicId);
            if (!topicExists)
            {
                return BadRequest("Invalid Topic ID.");
            }

            var createdCard = await _flashcardService.CreateFlashcardAsync(flashcard);
            return CreatedAtAction(nameof(GetFlashcardById), new { id = createdCard.Id }, createdCard);
        }

        // GET: api/flashcards/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Flashcard>> GetFlashcardById(int id)
        {
            var card = await _flashcardService.GetFlashcardByIdAsync(id);
            if (card == null) return NotFound();
            return Ok(card);
        }

        // POST: api/flashcards/5/review?quality=easy
        [HttpPost("{id}/review")]
        public async Task<ActionResult<Flashcard>> ReviewFlashcard(int id, [FromQuery] string quality)
        {
            var card = await _flashcardService.ReviewFlashcardAsync(id, quality);
            if (card == null) return NotFound();
            return Ok(card);
        }

        // DELETE: api/flashcards/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFlashcard(int id)
        {
            var deleted = await _flashcardService.DeleteFlashcardAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
