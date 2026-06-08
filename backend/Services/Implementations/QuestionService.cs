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

        public async Task<IEnumerable<Question>> GetQuestionsAsync(int? topicId, string? difficulty, int userId)
        {
            return await _questionRepository.GetByTopicIdAndDifficultyAsync(topicId, difficulty, userId);
        }

        public async Task<Question?> GetQuestionByIdAsync(int id, int userId)
        {
            return await _questionRepository.GetByIdAsync(id, userId);
        }

        public async Task<Question> CreateQuestionAsync(Question question, int userId)
        {
            question.UserId = userId;
            return await _questionRepository.AddAsync(question);
        }

        public async Task<Question?> UpdateQuestionStatusAsync(int id, string status, int userId)
        {
            return await _questionRepository.UpdateStatusAsync(id, status, userId);
        }

        public async Task<bool> DeleteQuestionAsync(int id, int userId)
        {
            return await _questionRepository.DeleteAsync(id, userId);
        }
    }
}
