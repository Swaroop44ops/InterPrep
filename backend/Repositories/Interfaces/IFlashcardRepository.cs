using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;

namespace backend.Repositories.Interfaces
{
    public interface IFlashcardRepository
    {
        Task<IEnumerable<Flashcard>> GetAllAsync(int userId);
        Task<IEnumerable<Flashcard>> GetByTopicIdAsync(int topicId, int userId);
        Task<Flashcard?> GetByIdAsync(int id, int userId);
        Task<Flashcard> AddAsync(Flashcard flashcard);
        Task<Flashcard> UpdateAsync(Flashcard flashcard);
        Task<bool> DeleteAsync(int id, int userId);
    }
}
