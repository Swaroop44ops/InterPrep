using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend.Models;
using backend.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [Authorize]
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

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !int.TryParse(claim.Value, out var userId))
            {
                throw new InvalidOperationException("User ID claim is missing or invalid.");
            }
            return userId;
        }

        // GET: api/questions?topicId=1&difficulty=Medium
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Question>>> GetQuestions(
            [FromQuery] int? topicId, 
            [FromQuery] string? difficulty)
        {
            var userId = GetUserId();
            var questions = await _questionService.GetQuestionsAsync(topicId, difficulty, userId);
            return Ok(questions);
        }

        // POST: api/questions
        [HttpPost]
        public async Task<ActionResult<Question>> CreateQuestion([FromBody] Question question)
        {
            var userId = GetUserId();
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var topicExists = await _topicService.TopicExistsAsync(question.TopicId);
            if (!topicExists)
            {
                return BadRequest("Invalid Topic ID.");
            }

            var createdQuestion = await _questionService.CreateQuestionAsync(question, userId);
            return CreatedAtAction(nameof(GetQuestionById), new { id = createdQuestion.Id }, createdQuestion);
        }

        // GET: api/questions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Question>> GetQuestionById(int id)
        {
            var userId = GetUserId();
            var question = await _questionService.GetQuestionByIdAsync(id, userId);
            if (question == null) return NotFound();
            return Ok(question);
        }

        // PUT: api/questions/5/status
        [HttpPut("{id}/status")]
        public async Task<ActionResult<Question>> UpdateQuestionStatus(int id, [FromBody] string status)
        {
            var userId = GetUserId();
            var updated = await _questionService.UpdateQuestionStatusAsync(id, status, userId);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        // DELETE: api/questions/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var userId = GetUserId();
            var deleted = await _questionService.DeleteQuestionAsync(id, userId);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
