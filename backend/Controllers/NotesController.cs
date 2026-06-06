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
        public async Task<ActionResult<IEnumerable<Note>>> GetNotes([FromQuery] int? topicId)
        {
            var notes = await _noteService.GetNotesAsync(topicId);
            return Ok(notes);
        }

        // GET: api/notes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Note>> GetNote(int id)
        {
            var note = await _noteService.GetNoteByIdAsync(id);
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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var topicExists = await _topicService.TopicExistsAsync(note.TopicId);
            if (!topicExists)
            {
                return BadRequest("Invalid Topic ID. Topic does not exist.");
            }

            var createdNote = await _noteService.CreateNoteAsync(note);
            return CreatedAtAction(nameof(GetNote), new { id = createdNote.Id }, createdNote);
        }

        // PUT: api/notes/5
        [HttpPut("{id}")]
        public async Task<ActionResult<Note>> UpdateNote(int id, [FromBody] Note updatedNote)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var topicExists = await _topicService.TopicExistsAsync(updatedNote.TopicId);
            if (!topicExists)
            {
                return BadRequest("Invalid Topic ID. Topic does not exist.");
            }

            var result = await _noteService.UpdateNoteAsync(id, updatedNote);
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
            var deleted = await _noteService.DeleteNoteAsync(id);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}
