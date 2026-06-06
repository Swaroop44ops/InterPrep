using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;

namespace backend.Services.Interfaces
{
    public interface INoteService
    {
        Task<IEnumerable<Note>> GetNotesAsync(int? topicId, int userId);
        Task<Note?> GetNoteByIdAsync(int id, int userId);
        Task<Note> CreateNoteAsync(Note note, int userId);
        Task<Note?> UpdateNoteAsync(int id, Note updatedNote, int userId);
        Task<bool> DeleteNoteAsync(int id, int userId);
    }
}
