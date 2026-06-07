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
    public class StudySessionsController : ControllerBase
    {
        private readonly IStudySessionService _studySessionService;

        public StudySessionsController(IStudySessionService studySessionService)
        {
            _studySessionService = studySessionService;
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

        // GET: api/studysessions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudySession>>> GetStudySessions()
        {
            var userId = GetUserId();
            var sessions = await _studySessionService.GetStudySessionsAsync(userId);
            return Ok(sessions);
        }

        // POST: api/studysessions
        [HttpPost]
        public async Task<ActionResult<StudySession>> LogStudySession([FromBody] StudySession session)
        {
            var userId = GetUserId();
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdSession = await _studySessionService.LogStudySessionAsync(session, userId);
            return CreatedAtAction(nameof(GetStudySessions), new { id = createdSession.Id }, createdSession);
        }

        // GET: api/studysessions/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var userId = GetUserId();
            var stats = await _studySessionService.GetStatsSummaryAsync(userId);
            return Ok(stats);
        }
    }
}
