using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Models;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;

namespace backend.Services.Implementations
{
    public class StudySessionService : IStudySessionService
    {
        private readonly IStudySessionRepository _studySessionRepository;
        private readonly IQuestionRepository _questionRepository;
        private readonly IFlashcardRepository _flashcardRepository;

        public StudySessionService(
            IStudySessionRepository studySessionRepository,
            IQuestionRepository questionRepository,
            IFlashcardRepository flashcardRepository)
        {
            _studySessionRepository = studySessionRepository;
            _questionRepository = questionRepository;
            _flashcardRepository = flashcardRepository;
        }

        public async Task<IEnumerable<StudySession>> GetStudySessionsAsync(int userId)
        {
            return await _studySessionRepository.GetAllAsync(userId);
        }

        public async Task<StudySession> LogStudySessionAsync(StudySession session, int userId)
        {
            session.UserId = userId;
            return await _studySessionRepository.AddAsync(session);
        }

        public async Task<object> GetStatsSummaryAsync(int userId)
        {
            // 1. Group historical sessions by date to build the heatmap data source
            var sessions = await _studySessionRepository.GetAllAsync(userId);
            var heatmap = sessions
                .GroupBy(s => s.CreatedAt.Date)
                .Select(g => new {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    count = g.Count(),
                    duration = g.Sum(s => s.DurationSeconds)
                })
                .ToList();

            // 2. Count confident questions per topic
            var questions = await _questionRepository.GetAllAsync(userId);
            var confidentQuestions = questions
                .Where(q => q.Status == "Confident")
                .GroupBy(q => q.TopicId)
                .Select(g => new {
                    topicId = g.Key,
                    count = g.Count()
                })
                .ToList();

            // 3. Count due flashcards per topic
            var flashcards = await _flashcardRepository.GetAllAsync(userId);
            var now = DateTime.UtcNow;
            var dueFlashcards = flashcards
                .Where(f => f.NextReviewDate <= now)
                .GroupBy(f => f.TopicId)
                .Select(g => new {
                    topicId = g.Key,
                    count = g.Count()
                })
                .ToList();

            return new {
                heatmap = heatmap,
                confidentQuestions = confidentQuestions,
                dueFlashcards = dueFlashcards
            };
        }
    }
}
