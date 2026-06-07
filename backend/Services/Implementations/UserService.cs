using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using backend.Data;
using backend.Models;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;

namespace backend.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly AppDbContext _context;

        public UserService(IUserRepository userRepository, AppDbContext context)
        {
            _userRepository = userRepository;
            _context = context;
        }

        public async Task<User?> RegisterAsync(string username, string password)
        {
            // Check if username is already taken
            var existingUser = await _userRepository.GetByUsernameAsync(username);
            if (existingUser != null)
            {
                return null; // Username already exists
            }

            var user = new User
            {
                Username = username,
                CreatedAt = DateTime.UtcNow
            };

            // Encrypt the password symmetrically using AES-256
            user.PasswordHash = EncryptionHelper.Encrypt(password);

            var createdUser = await _userRepository.AddAsync(user);

            try
            {
                // Clone default flashcards (from UserId = 1) for the new user
                var defaultFlashcards = await _context.Flashcards.AsNoTracking().Where(f => f.UserId == 1).ToListAsync();
                foreach (var card in defaultFlashcards)
                {
                    var newCard = new Flashcard
                    {
                        Front = card.Front,
                        Back = card.Back,
                        TopicId = card.TopicId,
                        IntervalDays = 1,
                        NextReviewDate = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UserId = createdUser.Id
                    };
                    _context.Flashcards.Add(newCard);
                }

                // Clone default questions (from UserId = 1) for the new user
                var defaultQuestions = await _context.Questions.AsNoTracking().Where(q => q.UserId == 1).ToListAsync();
                foreach (var q in defaultQuestions)
                {
                    var newQuestion = new Question
                    {
                        Text = q.Text,
                        Answer = q.Answer,
                        TopicId = q.TopicId,
                        Difficulty = q.Difficulty,
                        Status = "Unseen",
                        CreatedAt = DateTime.UtcNow,
                        UserId = createdUser.Id
                    };
                    _context.Questions.Add(newQuestion);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log exception (in real apps) but don't crash registration if cloning default items fails
                Console.WriteLine($"Error cloning default items: {ex.Message}");
            }

            return createdUser;
        }

        public async Task<User?> LoginAsync(string username, string password)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                return null; // User not found
            }

            // Decrypt stored password and compare
            var decryptedPassword = EncryptionHelper.Decrypt(user.PasswordHash);
            if (decryptedPassword != password)
            {
                return null; // Invalid password
            }

            return user;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllUsersAsync();
        }

        public async Task SaveRefreshTokenAsync(int userId, string token, DateTime expiry)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.RefreshToken = token;
                user.RefreshTokenExpiry = expiry;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<User?> GetUserByRefreshTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            return await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == token);
        }

        public async Task RevokeRefreshTokenAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;
                await _context.SaveChangesAsync();
            }
        }
    }
}
