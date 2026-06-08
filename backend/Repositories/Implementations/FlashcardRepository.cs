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

            // If Admin (UserId == 1) adds a flashcard, clone it for all other existing users
            if (flashcard.UserId == 1)
            {
                var otherUserIds = await _context.Users
                    .Where(u => u.Id != 1)
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var otherUserId in otherUserIds)
                {
                    var clonedCard = new Flashcard
                    {
                        Front = flashcard.Front,
                        Back = flashcard.Back,
                        TopicId = flashcard.TopicId,
                        IntervalDays = 1,
                        NextReviewDate = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UserId = otherUserId
                    };
                    _context.Flashcards.Add(clonedCard);
                }
                await _context.SaveChangesAsync();
            }

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

            // If the user is the Admin (UserId == 1), delete all other users' copies of this flashcard too
            if (userId == 1)
            {
                var copies = await _context.Flashcards
                    .Where(f => f.UserId != 1 && f.Front == card.Front && f.TopicId == card.TopicId)
                    .ToListAsync();
                _context.Flashcards.RemoveRange(copies);
            }

            _context.Flashcards.Remove(card);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
