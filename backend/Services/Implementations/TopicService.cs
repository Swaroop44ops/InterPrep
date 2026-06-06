using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;

namespace backend.Services.Implementations
{
    public class TopicService : ITopicService
    {
        private readonly ITopicRepository _topicRepository;

        public TopicService(ITopicRepository topicRepository)
        {
            _topicRepository = topicRepository;
        }

        public async Task<IEnumerable<Topic>> GetTopicsAsync()
        {
            return await _topicRepository.GetAllAsync();
        }

        public async Task<Topic?> GetTopicByIdAsync(int id)
        {
            return await _topicRepository.GetByIdAsync(id);
        }

        public async Task<bool> TopicExistsAsync(int id)
        {
            return await _topicRepository.ExistsAsync(id);
        }
    }
}
