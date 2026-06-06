using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;

namespace backend.Repositories.Interfaces
{
    public interface IStudySessionRepository
    {
        Task<IEnumerable<StudySession>> GetAllAsync();
        Task<StudySession> AddAsync(StudySession session);
    }
}
