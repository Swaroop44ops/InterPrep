using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.Services;
using backend.Services.Interfaces;
using System.Threading.Tasks;
using System.Linq;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        public class AuthRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AuthRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Username and Password are required.");
            }

            var user = await _userService.RegisterAsync(request.Username, request.Password);
            if (user == null)
            {
                return BadRequest("Username is already taken.");
            }

            // Return user details without password hash for security
            return Ok(new { id = user.Id, username = user.Username, createdAt = user.CreatedAt });
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Username and Password are required.");
            }

            var user = await _userService.LoginAsync(request.Username, request.Password);
            if (user == null)
            {
                return Unauthorized("Invalid username or password.");
            }

            return Ok(new { id = user.Id, username = user.Username, message = "Login successful!" });
        }

        // GET: api/auth/users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            var result = users.Select(u => new
            {
                id = u.Id,
                username = u.Username,
                createdAt = u.CreatedAt,
                encryptedPassword = u.PasswordHash,
                decryptedPassword = EncryptionHelper.Decrypt(u.PasswordHash)
            });
            return Ok(result);
        }
    }
}
