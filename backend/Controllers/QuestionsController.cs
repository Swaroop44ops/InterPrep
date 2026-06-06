using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuestionsController : ControllerBase
    {
        private readonly IQuestionService _questionService;
        private readonly ITopicService _topicService;

        public QuestionsController(IQuestionService questionService, ITopicService topicService)
        {
            _questionService = questionService;
            _topicService = topicService;
        }

        // GET: api/questions?topicId=1&difficulty=Medium
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Question>>> GetQuestions(
            [FromHeader(Name = "X-User-Id")] int? userId,
            [FromQuery] int? topicId, 
            [FromQuery] string? difficulty)
        {
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized("Missing or invalid user context (X-User-Id header).");
            }

            var questions = await _questionService.GetQuestionsAsync(topicId, difficulty, userId.Value);
            return Ok(questions);
        }

        // POST: api/questions
        [HttpPost]
        public async Task<ActionResult<Question>> CreateQuestion(
            [FromHeader(Name = "X-User-Id")] int? userId,
            [FromBody] Question question)
        {
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized("Missing or invalid user context (X-User-Id header).");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var topicExists = await _topicService.TopicExistsAsync(question.TopicId);
            if (!topicExists)
            {
                return BadRequest("Invalid Topic ID.");
            }

            var createdQuestion = await _questionService.CreateQuestionAsync(question, userId.Value);
            return CreatedAtAction(nameof(GetQuestionById), new { id = createdQuestion.Id, userId = userId.Value }, createdQuestion);
        }

        // GET: api/questions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Question>> GetQuestionById(
            [FromHeader(Name = "X-User-Id")] int? userId,
            int id)
        {
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized("Missing or invalid user context (X-User-Id header).");
            }

            var question = await _questionService.GetQuestionByIdAsync(id, userId.Value);
            if (question == null) return NotFound();
            return Ok(question);
        }

        // PUT: api/questions/5/status
        [HttpPut("{id}/status")]
        public async Task<ActionResult<Question>> UpdateQuestionStatus(
            [FromHeader(Name = "X-User-Id")] int? userId,
            int id,
            [FromBody] string status)
        {
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized("Missing or invalid user context (X-User-Id header).");
            }

            var updated = await _questionService.UpdateQuestionStatusAsync(id, status, userId.Value);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        // DELETE: api/questions/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuestion(
            [FromHeader(Name = "X-User-Id")] int? userId,
            int id)
        {
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized("Missing or invalid user context (X-User-Id header).");
            }

            var deleted = await _questionService.DeleteQuestionAsync(id, userId.Value);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
