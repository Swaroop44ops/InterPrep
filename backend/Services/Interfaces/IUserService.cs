using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;

namespace backend.Services.Interfaces
{
    public interface IUserService
    {
        Task<User?> RegisterAsync(string username, string password);
        Task<User?> LoginAsync(string username, string password);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task SaveRefreshTokenAsync(int userId, string token, DateTime expiry);
        Task<User?> GetUserByRefreshTokenAsync(string token);
        Task RevokeRefreshTokenAsync(int userId);
    }
}
