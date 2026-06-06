using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;

namespace backend.Services.Implementations
{
    public class FlashcardService : IFlashcardService
    {
        private readonly IFlashcardRepository _flashcardRepository;

        public FlashcardService(IFlashcardRepository flashcardRepository)
        {
            _flashcardRepository = flashcardRepository;
        }

        public async Task<IEnumerable<Flashcard>> GetFlashcardsAsync(int? topicId)
        {
            if (topicId.HasValue && topicId.Value > 0)
            {
                return await _flashcardRepository.GetByTopicIdAsync(topicId.Value);
            }
            return await _flashcardRepository.GetAllAsync();
        }

        public async Task<Flashcard?> GetFlashcardByIdAsync(int id)
        {
            return await _flashcardRepository.GetByIdAsync(id);
        }

        public async Task<Flashcard> CreateFlashcardAsync(Flashcard flashcard)
        {
            return await _flashcardRepository.AddAsync(flashcard);
        }

        public async Task<Flashcard?> ReviewFlashcardAsync(int id, string quality)
        {
            var card = await _flashcardRepository.GetByIdAsync(id);
            if (card == null) return null;

            if (string.Equals(quality, "easy", StringComparison.OrdinalIgnoreCase))
            {
                card.IntervalDays = Math.Max(1, card.IntervalDays * 2);
                card.NextReviewDate = DateTime.UtcNow.AddDays(card.IntervalDays);
            }
            else // "hard" or default
            {
                card.IntervalDays = 1;
                card.NextReviewDate = DateTime.UtcNow.AddDays(1);
            }

            return await _flashcardRepository.UpdateAsync(card);
        }

        public async Task<bool> DeleteFlashcardAsync(int id)
        {
            return await _flashcardRepository.DeleteAsync(id);
        }
    }
}
