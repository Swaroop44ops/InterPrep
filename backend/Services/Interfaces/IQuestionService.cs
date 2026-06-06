using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;

namespace backend.Services.Interfaces
{
    public interface IQuestionService
    {
        Task<IEnumerable<Question>> GetQuestionsAsync(int? topicId, string? difficulty);
        Task<Question?> GetQuestionByIdAsync(int id);
        Task<Question> CreateQuestionAsync(Question question);
        Task<Question?> UpdateQuestionStatusAsync(int id, string status);
        Task<bool> DeleteQuestionAsync(int id);
    }
}
