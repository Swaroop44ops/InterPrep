using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using backend.Repositories.Interfaces;

namespace backend.Repositories.Implementations
{
    public class StudySessionRepository : IStudySessionRepository
    {
        private readonly AppDbContext _context;

        public StudySessionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<StudySession>> GetAllAsync(int userId)
        {
            return await _context.StudySessions.Where(s => s.UserId == userId).ToListAsync();
        }

        public async Task<StudySession> AddAsync(StudySession session)
        {
            _context.StudySessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }
    }
}
