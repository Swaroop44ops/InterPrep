using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;

namespace backend.Services.Interfaces
{
    public interface IStudySessionService
    {
        Task<IEnumerable<StudySession>> GetStudySessionsAsync(int userId);
        Task<StudySession> LogStudySessionAsync(StudySession session, int userId);
        Task<object> GetStatsSummaryAsync(int userId);
    }
}
