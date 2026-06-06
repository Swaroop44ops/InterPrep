using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;

namespace backend.Repositories.Interfaces
{
    public interface INoteRepository
    {
        Task<IEnumerable<Note>> GetAllAsync();
        Task<IEnumerable<Note>> GetByTopicIdAsync(int topicId);
        Task<Note?> GetByIdAsync(int id);
        Task<Note> AddAsync(Note note);
        Task<Note> UpdateAsync(Note note);
        Task<bool> DeleteAsync(int id);
    }
}
