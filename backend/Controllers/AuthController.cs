using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using backend.Models;
using backend.Services;
using backend.Services.Interfaces;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserService userService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        public class AuthRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AuthRequest? request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Request body is required." });
            }

            try
            {
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new { message = "Username and Password are required." });
                }

                var user = await _userService.RegisterAsync(request.Username, request.Password);
                if (user == null)
                {
                    return BadRequest(new { message = "Username is already taken." });
                }

                return Ok(new { id = user.Id, username = user.Username, createdAt = user.CreatedAt });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for username {Username}", request.Username);
                return StatusCode(500, new { message = "Registration failed. Please try again." });
            }
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthRequest? request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Request body is required." });
            }

            try
            {
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new { message = "Username and Password are required." });
                }

                var user = await _userService.LoginAsync(request.Username, request.Password);
                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid username or password." });
                }

                return Ok(new { id = user.Id, username = user.Username, message = "Login successful!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for username {Username}", request.Username);
                return StatusCode(500, new { message = "Login failed. Please try again." });
            }
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
