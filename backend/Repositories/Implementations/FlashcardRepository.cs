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

        public async Task<IEnumerable<Flashcard>> GetAllAsync()
        {
            return await _context.Flashcards.ToListAsync();
        }

        public async Task<IEnumerable<Flashcard>> GetByTopicIdAsync(int topicId)
        {
            return await _context.Flashcards.Where(f => f.TopicId == topicId).ToListAsync();
        }

        public async Task<Flashcard?> GetByIdAsync(int id)
        {
            return await _context.Flashcards.FindAsync(id);
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

        public async Task<bool> DeleteAsync(int id)
        {
            var card = await _context.Flashcards.FindAsync(id);
            if (card == null) return false;

            _context.Flashcards.Remove(card);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
