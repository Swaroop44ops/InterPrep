using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;

namespace backend.Services.Implementations
{
    public class QuestionService : IQuestionService
    {
        private readonly IQuestionRepository _questionRepository;

        public QuestionService(IQuestionRepository questionRepository)
        {
            _questionRepository = questionRepository;
        }

        public async Task<IEnumerable<Question>> GetQuestionsAsync(int? topicId, string? difficulty)
        {
            return await _questionRepository.GetByTopicIdAndDifficultyAsync(topicId, difficulty);
        }

        public async Task<Question?> GetQuestionByIdAsync(int id)
        {
            return await _questionRepository.GetByIdAsync(id);
        }

        public async Task<Question> CreateQuestionAsync(Question question)
        {
            return await _questionRepository.AddAsync(question);
        }

        public async Task<Question?> UpdateQuestionStatusAsync(int id, string status)
        {
            var question = await _questionRepository.GetByIdAsync(id);
            if (question == null) return null;

            var formattedStatus = string.Empty;
            if (string.Equals(status, "confident", StringComparison.OrdinalIgnoreCase))
                formattedStatus = "Confident";
            else if (string.Equals(status, "attempted", StringComparison.OrdinalIgnoreCase))
                formattedStatus = "Attempted";
            else
                formattedStatus = "Unseen";

            question.Status = formattedStatus;
            return await _questionRepository.UpdateAsync(question);
        }

        public async Task<bool> DeleteQuestionAsync(int id)
        {
            return await _questionRepository.DeleteAsync(id);
        }
    }
}
