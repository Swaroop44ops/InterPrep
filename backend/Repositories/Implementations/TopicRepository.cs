using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using backend.Repositories.Interfaces;

namespace backend.Repositories.Implementations
{
    public class TopicRepository : ITopicRepository
    {
        private readonly AppDbContext _context;

        public TopicRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Topic>> GetAllAsync()
        {
            return await _context.Topics.ToListAsync();
        }

        public async Task<Topic?> GetByIdAsync(int id)
        {
            return await _context.Topics.FindAsync(id);
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Topics.AnyAsync(t => t.Id == id);
        }
    }
}
