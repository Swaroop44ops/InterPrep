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
    public class NotesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/notes?topicId=1
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Note>>> GetNotes([FromQuery] int? topicId)
        {
            IQueryable<Note> query = _context.Notes;
            if (topicId.HasValue && topicId.Value > 0)
            {
                query = query.Where(n => n.TopicId == topicId.Value);
            }
            
            return await query.OrderByDescending(n => n.UpdatedAt).ToListAsync();
        }

        // GET: api/notes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Note>> GetNote(int id)
        {
            var note = await _context.Notes.FindAsync(id);

            if (note == null)
            {
                return NotFound();
            }

            return note;
        }

        // POST: api/notes
        [HttpPost]
        public async Task<ActionResult<Note>> CreateNote([FromBody] Note note)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verify topic exists before attaching note to it
            var topicExists = await _context.Topics.AnyAsync(t => t.Id == note.TopicId);
            if (!topicExists)
            {
                return BadRequest("Invalid Topic ID. Topic does not exist.");
            }

            note.CreatedAt = DateTime.UtcNow;
            note.UpdatedAt = DateTime.UtcNow;

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetNote), new { id = note.Id }, note);
        }

        // PUT: api/notes/5
        [HttpPut("{id}")]
        public async Task<ActionResult<Note>> UpdateNote(int id, [FromBody] Note updatedNote)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var note = await _context.Notes.FindAsync(id);
            if (note == null)
            {
                return NotFound("Note not found");
            }

            var topicExists = await _context.Topics.AnyAsync(t => t.Id == updatedNote.TopicId);
            if (!topicExists)
            {
                return BadRequest("Invalid Topic ID. Topic does not exist.");
            }

            note.Title = updatedNote.Title;
            note.Content = updatedNote.Content;
            note.TopicId = updatedNote.TopicId;
            note.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NoteExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(note);
        }

        // DELETE: api/notes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNote(int id)
        {
            var note = await _context.Notes.FindAsync(id);
            if (note == null)
            {
                return NotFound();
            }

            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool NoteExists(int id)
        {
            return _context.Notes.Any(e => e.Id == id);
        }
    }
}
