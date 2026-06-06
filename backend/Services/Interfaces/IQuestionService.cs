using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;

namespace backend.Services.Interfaces
{
    public interface IQuestionService
    {
        Task<IEnumerable<Question>> GetQuestionsAsync(int? topicId, string? difficulty, int userId);
        Task<Question?> GetQuestionByIdAsync(int id, int userId);
        Task<Question> CreateQuestionAsync(Question question, int userId);
        Task<Question?> UpdateQuestionStatusAsync(int id, string status, int userId);
        Task<bool> DeleteQuestionAsync(int id, int userId);
    }
}
