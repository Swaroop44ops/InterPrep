using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;

namespace backend.Repositories.Interfaces
{
    public interface IFlashcardRepository
    {
        Task<IEnumerable<Flashcard>> GetAllAsync();
        Task<IEnumerable<Flashcard>> GetByTopicIdAsync(int topicId);
        Task<Flashcard?> GetByIdAsync(int id);
        Task<Flashcard> AddAsync(Flashcard flashcard);
        Task<Flashcard> UpdateAsync(Flashcard flashcard);
        Task<bool> DeleteAsync(int id);
    }
}
