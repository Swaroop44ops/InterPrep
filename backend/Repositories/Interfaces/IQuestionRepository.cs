using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;

namespace backend.Repositories.Interfaces
{
    public interface IQuestionRepository
    {
        Task<IEnumerable<Question>> GetAllAsync();
        Task<IEnumerable<Question>> GetByTopicIdAndDifficultyAsync(int? topicId, string? difficulty);
        Task<Question?> GetByIdAsync(int id);
        Task<Question> AddAsync(Question question);
        Task<Question> UpdateAsync(Question question);
        Task<bool> DeleteAsync(int id);
    }
}
