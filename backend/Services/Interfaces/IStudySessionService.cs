using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;

namespace backend.Services.Interfaces
{
    public interface IStudySessionService
    {
        Task<IEnumerable<StudySession>> GetStudySessionsAsync();
        Task<StudySession> LogStudySessionAsync(StudySession session);
        Task<object> GetStatsSummaryAsync();
    }
}
