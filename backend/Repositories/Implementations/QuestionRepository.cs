using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using backend.Repositories.Interfaces;

namespace backend.Repositories.Implementations
{
    public class QuestionRepository : IQuestionRepository
    {
        private readonly AppDbContext _context;

        public QuestionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Question>> GetAllAsync(int userId)
        {
            var questions = await _context.Questions
                .Where(q => q.UserId == userId || q.UserId == 1)
                .ToListAsync();

            await MapStatusesAsync(questions, userId);
            return questions;
        }

        public async Task<IEnumerable<Question>> GetByTopicIdAndDifficultyAsync(int? topicId, string? difficulty, int userId)
        {
            IQueryable<Question> query = _context.Questions.Where(q => q.UserId == userId || q.UserId == 1);
            if (topicId.HasValue && topicId.Value > 0)
            {
                query = query.Where(q => q.TopicId == topicId.Value);
            }

            if (!string.IsNullOrEmpty(difficulty))
            {
                query = query.Where(q => q.Difficulty.ToLower() == difficulty.ToLower());
            }

            var questions = await query.ToListAsync();
            await MapStatusesAsync(questions, userId);
            return questions;
        }

        public async Task<Question?> GetByIdAsync(int id, int userId)
        {
            var question = await _context.Questions
                .FirstOrDefaultAsync(q => q.Id == id && (q.UserId == userId || q.UserId == 1));

            if (question != null)
            {
                var userStatus = await _context.UserQuestionStatuses
                    .FirstOrDefaultAsync(us => us.UserId == userId && us.QuestionId == id);
                question.Status = userStatus?.Status ?? "Unseen";
            }

            return question;
        }

        public async Task<Question> AddAsync(Question question)
        {
            _context.Questions.Add(question);
            await _context.SaveChangesAsync();
            return question;
        }

        public async Task<Question> UpdateAsync(Question question)
        {
            _context.Entry(question).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return question;
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            var question = await _context.Questions.FirstOrDefaultAsync(q => q.Id == id && q.UserId == userId);
            if (question == null) return false;

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Question?> UpdateStatusAsync(int id, string status, int userId)
        {
            // Verify the question exists and is accessible
            var question = await GetByIdAsync(id, userId);
            if (question == null) return null;

            var formattedStatus = "Unseen";
            if (string.Equals(status, "confident", StringComparison.OrdinalIgnoreCase))
                formattedStatus = "Confident";
            else if (string.Equals(status, "attempted", StringComparison.OrdinalIgnoreCase))
                formattedStatus = "Attempted";

            var userStatus = await _context.UserQuestionStatuses
                .FirstOrDefaultAsync(us => us.UserId == userId && us.QuestionId == id);

            if (userStatus == null)
            {
                userStatus = new UserQuestionStatus
                {
                    UserId = userId,
                    QuestionId = id,
                    Status = formattedStatus,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.UserQuestionStatuses.Add(userStatus);
            }
            else
            {
                userStatus.Status = formattedStatus;
                userStatus.UpdatedAt = DateTime.UtcNow;
                _context.Entry(userStatus).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
            question.Status = formattedStatus;
            return question;
        }

        private async Task MapStatusesAsync(List<Question> questions, int userId)
        {
            if (questions.Count == 0) return;

            var questionIds = questions.Select(q => q.Id).ToList();
            var statuses = await _context.UserQuestionStatuses
                .Where(us => us.UserId == userId && questionIds.Contains(us.QuestionId))
                .ToDictionaryAsync(us => us.QuestionId, us => us.Status);

            foreach (var q in questions)
            {
                q.Status = statuses.TryGetValue(q.Id, out var status) ? status : "Unseen";
            }
        }
    }
}
