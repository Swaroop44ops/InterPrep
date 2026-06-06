using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;

namespace backend.Services.Interfaces
{
    public interface INoteService
    {
        Task<IEnumerable<Note>> GetNotesAsync(int? topicId);
        Task<Note?> GetNoteByIdAsync(int id);
        Task<Note> CreateNoteAsync(Note note);
        Task<Note?> UpdateNoteAsync(int id, Note updatedNote);
        Task<bool> DeleteNoteAsync(int id);
    }
}
