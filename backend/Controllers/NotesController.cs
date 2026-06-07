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
    public class NotesController : ControllerBase
    {
        private readonly INoteService _noteService;
        private readonly ITopicService _topicService;

        public NotesController(INoteService noteService, ITopicService topicService)
        {
            _noteService = noteService;
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

        // GET: api/notes?topicId=1
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Note>>> GetNotes([FromQuery] int? topicId)
        {
            var userId = GetUserId();
            var notes = await _noteService.GetNotesAsync(topicId, userId);
            return Ok(notes);
        }

        // GET: api/notes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Note>> GetNote(int id)
        {
            var userId = GetUserId();
            var note = await _noteService.GetNoteByIdAsync(id, userId);
            if (note == null)
            {
                return NotFound();
            }
            return Ok(note);
        }

        // POST: api/notes
        [HttpPost]
        public async Task<ActionResult<Note>> CreateNote([FromBody] Note note)
        {
            var userId = GetUserId();
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var topicExists = await _topicService.TopicExistsAsync(note.TopicId);
            if (!topicExists)
            {
                return BadRequest("Invalid Topic ID. Topic does not exist.");
            }

            var createdNote = await _noteService.CreateNoteAsync(note, userId);
            return CreatedAtAction(nameof(GetNote), new { id = createdNote.Id }, createdNote);
        }

        // PUT: api/notes/5
        [HttpPut("{id}")]
        public async Task<ActionResult<Note>> UpdateNote(int id, [FromBody] Note updatedNote)
        {
            var userId = GetUserId();
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var topicExists = await _topicService.TopicExistsAsync(updatedNote.TopicId);
            if (!topicExists)
            {
                return BadRequest("Invalid Topic ID. Topic does not exist.");
            }

            var result = await _noteService.UpdateNoteAsync(id, updatedNote, userId);
            if (result == null)
            {
                return NotFound("Note not found");
            }

            return Ok(result);
        }

        // DELETE: api/notes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNote(int id)
        {
            var userId = GetUserId();
            var deleted = await _noteService.DeleteNoteAsync(id, userId);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}
