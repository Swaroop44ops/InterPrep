using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;

namespace backend.Services.Interfaces
{
    public interface ITopicService
    {
        Task<IEnumerable<Topic>> GetTopicsAsync();
        Task<Topic?> GetTopicByIdAsync(int id);
        Task<bool> TopicExistsAsync(int id);
    }
}
