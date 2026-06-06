using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using backend.Repositories.Interfaces;

namespace backend.Repositories.Implementations
{
    public class FlashcardRepository : IFlashcardRepository
    {
        private readonly AppDbContext _context;

        public FlashcardRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Flashcard>> GetAllAsync(int userId)
        {
            return await _context.Flashcards.Where(f => f.UserId == userId).ToListAsync();
        }

        public async Task<IEnumerable<Flashcard>> GetByTopicIdAsync(int topicId, int userId)
        {
            return await _context.Flashcards.Where(f => f.TopicId == topicId && f.UserId == userId).ToListAsync();
        }

        public async Task<Flashcard?> GetByIdAsync(int id, int userId)
        {
            return await _context.Flashcards.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
        }

        public async Task<Flashcard> AddAsync(Flashcard flashcard)
        {
            _context.Flashcards.Add(flashcard);
            await _context.SaveChangesAsync();
            return flashcard;
        }

        public async Task<Flashcard> UpdateAsync(Flashcard flashcard)
        {
            _context.Entry(flashcard).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return flashcard;
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            var card = await _context.Flashcards.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
            if (card == null) return false;

            _context.Flashcards.Remove(card);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
