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
        public async Task<ActionResult<IEnumerable<StudySession>>> GetStudySessions()
        {
            var sessions = await _studySessionService.GetStudySessionsAsync();
            return Ok(sessions);
        }

        // POST: api/studysessions
        [HttpPost]
        public async Task<ActionResult<StudySession>> LogStudySession([FromBody] StudySession session)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdSession = await _studySessionService.LogStudySessionAsync(session);
            return CreatedAtAction(nameof(GetStudySessions), new { id = createdSession.Id }, createdSession);
        }

        // GET: api/studysessions/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _studySessionService.GetStatsSummaryAsync();
            return Ok(stats);
        }
    }
}
