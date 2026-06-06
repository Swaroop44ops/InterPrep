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
            [FromQuery] int? topicId, 
            [FromQuery] string? difficulty)
        {
            var questions = await _questionService.GetQuestionsAsync(topicId, difficulty);
            return Ok(questions);
        }

        // POST: api/questions
        [HttpPost]
        public async Task<ActionResult<Question>> CreateQuestion([FromBody] Question question)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var topicExists = await _topicService.TopicExistsAsync(question.TopicId);
            if (!topicExists)
            {
                return BadRequest("Invalid Topic ID.");
            }

            var createdQuestion = await _questionService.CreateQuestionAsync(question);
            return CreatedAtAction(nameof(GetQuestionById), new { id = createdQuestion.Id }, createdQuestion);
        }

        // GET: api/questions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Question>> GetQuestionById(int id)
        {
            var question = await _questionService.GetQuestionByIdAsync(id);
            if (question == null) return NotFound();
            return Ok(question);
        }

        // PUT: api/questions/5/status
        [HttpPut("{id}/status")]
        public async Task<ActionResult<Question>> UpdateQuestionStatus(int id, [FromBody] string status)
        {
            var updated = await _questionService.UpdateQuestionStatusAsync(id, status);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        // DELETE: api/questions/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var deleted = await _questionService.DeleteQuestionAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
