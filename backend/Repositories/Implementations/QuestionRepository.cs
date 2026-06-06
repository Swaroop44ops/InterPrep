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
            return await _context.Questions.Where(q => q.UserId == userId).ToListAsync();
        }

        public async Task<IEnumerable<Question>> GetByTopicIdAndDifficultyAsync(int? topicId, string? difficulty, int userId)
        {
            IQueryable<Question> query = _context.Questions.Where(q => q.UserId == userId);
            if (topicId.HasValue && topicId.Value > 0)
            {
                query = query.Where(q => q.TopicId == topicId.Value);
            }

            if (!string.IsNullOrEmpty(difficulty))
            {
                query = query.Where(q => q.Difficulty.ToLower() == difficulty.ToLower());
            }

            return await query.ToListAsync();
        }

        public async Task<Question?> GetByIdAsync(int id, int userId)
        {
            return await _context.Questions.FirstOrDefaultAsync(q => q.Id == id && q.UserId == userId);
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
    }
}
