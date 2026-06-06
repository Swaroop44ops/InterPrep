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
            var topics = await _context.Topics.ToListAsync();
            return Ok(topics);
        }
    }
}
