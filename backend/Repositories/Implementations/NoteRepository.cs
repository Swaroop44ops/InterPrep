using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using backend.Repositories.Interfaces;

namespace backend.Repositories.Implementations
{
    public class NoteRepository : INoteRepository
    {
        private readonly AppDbContext _context;

        public NoteRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Note>> GetAllAsync(int userId)
        {
            return await _context.Notes.Where(n => n.UserId == userId || n.IsPublic).ToListAsync();
        }

        public async Task<IEnumerable<Note>> GetByTopicIdAsync(int topicId, int userId)
        {
            return await _context.Notes.Where(n => n.TopicId == topicId && (n.UserId == userId || n.IsPublic)).ToListAsync();
        }

        public async Task<Note?> GetByIdAsync(int id, int userId)
        {
            return await _context.Notes.FirstOrDefaultAsync(n => n.Id == id && (n.UserId == userId || n.IsPublic));
        }

        public async Task<Note> AddAsync(Note note)
        {
            _context.Notes.Add(note);
            await _context.SaveChangesAsync();
            return note;
        }

        public async Task<Note> UpdateAsync(Note note)
        {
            _context.Entry(note).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return note;
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            var note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            if (note == null) return false;

            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
