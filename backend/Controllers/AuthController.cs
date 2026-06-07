using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
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

                // Generate tokens
                var accessToken = JwtHelper.GenerateAccessToken(user.Id, user.Username);
                var refreshToken = JwtHelper.GenerateRefreshToken();
                var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

                // Save Refresh Token to Database
                await _userService.SaveRefreshTokenAsync(user.Id, refreshToken, refreshTokenExpiry);

                // Write HttpOnly Cookie
                SetRefreshTokenCookie(refreshToken, refreshTokenExpiry);

                return Ok(new 
                { 
                    id = user.Id, 
                    username = user.Username, 
                    accessToken = accessToken,
                    message = "Login successful!" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for username {Username}", request.Username);
                return StatusCode(500, new { message = "Login failed. Please try again." });
            }
        }

        // POST: api/auth/refresh
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            // Read cookie
            if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken) || string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized(new { message = "Refresh token is missing." });
            }

            try
            {
                var user = await _userService.GetUserByRefreshTokenAsync(refreshToken);
                if (user == null || user.RefreshTokenExpiry == null || user.RefreshTokenExpiry.Value <= DateTime.UtcNow)
                {
                    return Unauthorized(new { message = "Invalid or expired refresh token." });
                }

                // Generate new access and rotated refresh token
                var newAccessToken = JwtHelper.GenerateAccessToken(user.Id, user.Username);
                var newRefreshToken = JwtHelper.GenerateRefreshToken();
                var newExpiry = DateTime.UtcNow.AddDays(7);

                await _userService.SaveRefreshTokenAsync(user.Id, newRefreshToken, newExpiry);
                SetRefreshTokenCookie(newRefreshToken, newExpiry);

                return Ok(new
                {
                    accessToken = newAccessToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed.");
                return StatusCode(500, new { message = "Refresh failed. Please log in again." });
            }
        }

        // POST: api/auth/logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // Read refresh token to revoke it in the database
            if (Request.Cookies.TryGetValue("refreshToken", out var token) && !string.IsNullOrEmpty(token))
            {
                var user = await _userService.GetUserByRefreshTokenAsync(token);
                if (user != null)
                {
                    await _userService.RevokeRefreshTokenAsync(user.Id);
                }
            }

            // Delete cookie
            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });

            return Ok(new { message = "Logged out successfully" });
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

        private void SetRefreshTokenCookie(string token, DateTime expiry)
        {
            Response.Cookies.Append("refreshToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Must be true when SameSite = None
                SameSite = SameSiteMode.None, // Must be None for cross-domain Vercel -> Render calls
                Expires = expiry
            });
        }
    }
}
