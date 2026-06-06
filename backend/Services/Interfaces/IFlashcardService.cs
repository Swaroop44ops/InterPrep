using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;

namespace backend.Services.Interfaces
{
    public interface IFlashcardService
    {
        Task<IEnumerable<Flashcard>> GetFlashcardsAsync(int? topicId);
        Task<Flashcard?> GetFlashcardByIdAsync(int id);
        Task<Flashcard> CreateFlashcardAsync(Flashcard flashcard);
        Task<Flashcard?> ReviewFlashcardAsync(int id, string quality);
        Task<bool> DeleteFlashcardAsync(int id);
    }
}
