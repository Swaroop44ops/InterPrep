using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;

namespace backend.Repositories.Interfaces
{
    public interface ITopicRepository
    {
        Task<IEnumerable<Topic>> GetAllAsync();
        Task<Topic?> GetByIdAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}
