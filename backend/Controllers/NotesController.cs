using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotesController : ControllerBase
    {
        private readonly INoteService _noteService;
        private readonly ITopicService _topicService;

        public NotesController(INoteService noteService, ITopicService topicService)
        {
            _noteService = noteService;
            _topicService = topicService;
        }

        // GET: api/notes?topicId=1
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Note>>> GetNotes(
            [FromHeader(Name = "X-User-Id")] int? userId,
            [FromQuery] int? topicId)
        {
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized("Missing or invalid user context (X-User-Id header).");
            }

            var notes = await _noteService.GetNotesAsync(topicId, userId.Value);
            return Ok(notes);
        }

        // GET: api/notes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Note>> GetNote(
            [FromHeader(Name = "X-User-Id")] int? userId,
            int id)
        {
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized("Missing or invalid user context (X-User-Id header).");
            }

            var note = await _noteService.GetNoteByIdAsync(id, userId.Value);
            if (note == null)
            {
                return NotFound();
            }
            return Ok(note);
        }

        // POST: api/notes
        [HttpPost]
        public async Task<ActionResult<Note>> CreateNote(
            [FromHeader(Name = "X-User-Id")] int? userId,
            [FromBody] Note note)
        {
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized("Missing or invalid user context (X-User-Id header).");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var topicExists = await _topicService.TopicExistsAsync(note.TopicId);
            if (!topicExists)
            {
                return BadRequest("Invalid Topic ID. Topic does not exist.");
            }

            var createdNote = await _noteService.CreateNoteAsync(note, userId.Value);
            return CreatedAtAction(nameof(GetNote), new { id = createdNote.Id }, createdNote);
        }

        // PUT: api/notes/5
        [HttpPut("{id}")]
        public async Task<ActionResult<Note>> UpdateNote(
            [FromHeader(Name = "X-User-Id")] int? userId,
            int id,
            [FromBody] Note updatedNote)
        {
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized("Missing or invalid user context (X-User-Id header).");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var topicExists = await _topicService.TopicExistsAsync(updatedNote.TopicId);
            if (!topicExists)
            {
                return BadRequest("Invalid Topic ID. Topic does not exist.");
            }

            var result = await _noteService.UpdateNoteAsync(id, updatedNote, userId.Value);
            if (result == null)
            {
                return NotFound("Note not found");
            }

            return Ok(result);
        }

        // DELETE: api/notes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNote(
            [FromHeader(Name = "X-User-Id")] int? userId,
            int id)
        {
            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized("Missing or invalid user context (X-User-Id header).");
            }

            var deleted = await _noteService.DeleteNoteAsync(id, userId.Value);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}
