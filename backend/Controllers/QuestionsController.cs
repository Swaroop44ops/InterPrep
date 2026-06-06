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
    public class QuestionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public QuestionsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/questions?topicId=1&difficulty=Medium
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Question>>> GetQuestions(
            [FromQuery] int? topicId, 
            [FromQuery] string? difficulty)
        {
            IQueryable<Question> query = _context.Questions;
            if (topicId.HasValue && topicId.Value > 0)
            {
                query = query.Where(q => q.TopicId == topicId.Value);
            }

            if (!string.IsNullOrEmpty(difficulty))
            {
                query = query.Where(q => q.Difficulty.ToLower() == difficulty.ToLower());
            }

            return await query.ToListAsync();
        }

        // POST: api/questions
        [HttpPost]
        public async Task<ActionResult<Question>> CreateQuestion([FromBody] Question question)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var topicExists = await _context.Topics.AnyAsync(t => t.Id == question.TopicId);
            if (!topicExists)
            {
                return BadRequest("Invalid Topic ID.");
            }

            question.Status = "Unseen"; // Default
            question.CreatedAt = DateTime.UtcNow;

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetQuestionById), new { id = question.Id }, question);
        }

        // GET: api/questions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Question>> GetQuestionById(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null) return NotFound();
            return question;
        }

        // PUT: api/questions/5/status
        [HttpPut("{id}/status")]
        public async Task<ActionResult<Question>> UpdateQuestionStatus(int id, [FromBody] string status)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null)
            {
                return NotFound();
            }

            // Standardize and validate status value
            var formattedStatus = string.Empty;
            if (string.Equals(status, "confident", StringComparison.OrdinalIgnoreCase))
                formattedStatus = "Confident";
            else if (string.Equals(status, "attempted", StringComparison.OrdinalIgnoreCase))
                formattedStatus = "Attempted";
            else
                formattedStatus = "Unseen";

            question.Status = formattedStatus;
            await _context.SaveChangesAsync();

            return Ok(question);
        }

        // DELETE: api/questions/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null) return NotFound();

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
