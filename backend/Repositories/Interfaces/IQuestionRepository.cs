using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;

namespace backend.Repositories.Interfaces
{
    public interface IQuestionRepository
    {
        Task<IEnumerable<Question>> GetAllAsync(int userId);
        Task<IEnumerable<Question>> GetByTopicIdAndDifficultyAsync(int? topicId, string? difficulty, int userId);
        Task<Question?> GetByIdAsync(int id, int userId);
        Task<Question> AddAsync(Question question);
        Task<Question> UpdateAsync(Question question);
        Task<bool> DeleteAsync(int id, int userId);
        Task<Question?> UpdateStatusAsync(int id, string status, int userId);
    }
}
