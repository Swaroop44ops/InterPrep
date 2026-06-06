using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudySessionsController : ControllerBase
    {
        private readonly IStudySessionService _studySessionService;

        public StudySessionsController(IStudySessionService studySessionService)
        {
            _studySessionService = studySessionService;
        }

        // GET: api/studysessions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudySession>>> GetStudySessions([FromHeader(Name = "X-User-Id")] int? userId)
        {
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized("Missing or invalid user context (X-User-Id header).");
            }

            var sessions = await _studySessionService.GetStudySessionsAsync(userId.Value);
            return Ok(sessions);
        }

        // POST: api/studysessions
        [HttpPost]
        public async Task<ActionResult<StudySession>> LogStudySession(
            [FromHeader(Name = "X-User-Id")] int? userId,
            [FromBody] StudySession session)
        {
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized("Missing or invalid user context (X-User-Id header).");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdSession = await _studySessionService.LogStudySessionAsync(session, userId.Value);
            return CreatedAtAction(nameof(GetStudySessions), new { id = createdSession.Id, userId = userId.Value }, createdSession);
        }

        // GET: api/studysessions/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats([FromHeader(Name = "X-User-Id")] int? userId)
        {
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized("Missing or invalid user context (X-User-Id header).");
            }

            var stats = await _studySessionService.GetStatsSummaryAsync(userId.Value);
            return Ok(stats);
        }
    }
}
