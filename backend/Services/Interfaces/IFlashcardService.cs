using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;

namespace backend.Services.Interfaces
{
    public interface IFlashcardService
    {
        Task<IEnumerable<Flashcard>> GetFlashcardsAsync(int? topicId, int userId);
        Task<Flashcard?> GetFlashcardByIdAsync(int id, int userId);
        Task<Flashcard> CreateFlashcardAsync(Flashcard flashcard, int userId);
        Task<Flashcard?> ReviewFlashcardAsync(int id, string quality, int userId);
        Task<bool> DeleteFlashcardAsync(int id, int userId);
    }
}
