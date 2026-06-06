using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TopicsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TopicsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/topics
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Topic>>> GetTopics()
        {
            // Automatically creates the database and seeds the data if they don't exist yet.
            // This simplifies the initial verification process for Phase 1.
            await _context.Database.EnsureCreatedAsync();
            
            var topics = await _context.Topics.ToListAsync();
            return Ok(topics);
        }
    }
}
