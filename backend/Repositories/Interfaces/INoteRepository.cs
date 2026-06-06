using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;

namespace backend.Repositories.Interfaces
{
    public interface INoteRepository
    {
        Task<IEnumerable<Note>> GetAllAsync(int userId);
        Task<IEnumerable<Note>> GetByTopicIdAsync(int topicId, int userId);
        Task<Note?> GetByIdAsync(int id, int userId);
        Task<Note> AddAsync(Note note);
        Task<Note> UpdateAsync(Note note);
        Task<bool> DeleteAsync(int id, int userId);
    }
}
