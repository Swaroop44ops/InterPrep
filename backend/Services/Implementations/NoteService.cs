using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Models;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;

namespace backend.Services.Implementations
{
    public class NoteService : INoteService
    {
        private readonly INoteRepository _noteRepository;

        public NoteService(INoteRepository noteRepository)
        {
            _noteRepository = noteRepository;
        }

        public async Task<IEnumerable<Note>> GetNotesAsync(int? topicId)
        {
            if (topicId.HasValue && topicId.Value > 0)
            {
                return await _noteRepository.GetByTopicIdAsync(topicId.Value);
            }
            return await _noteRepository.GetAllAsync();
        }

        public async Task<Note?> GetNoteByIdAsync(int id)
        {
            return await _noteRepository.GetByIdAsync(id);
        }

        public async Task<Note> CreateNoteAsync(Note note)
        {
            return await _noteRepository.AddAsync(note);
        }

        public async Task<Note?> UpdateNoteAsync(int id, Note updatedNote)
        {
            var existingNote = await _noteRepository.GetByIdAsync(id);
            if (existingNote == null) return null;

            existingNote.Title = updatedNote.Title;
            existingNote.Content = updatedNote.Content;
            existingNote.TopicId = updatedNote.TopicId;
            existingNote.UpdatedAt = DateTime.UtcNow;

            return await _noteRepository.UpdateAsync(existingNote);
        }

        public async Task<bool> DeleteNoteAsync(int id)
        {
            return await _noteRepository.DeleteAsync(id);
        }
    }
}
