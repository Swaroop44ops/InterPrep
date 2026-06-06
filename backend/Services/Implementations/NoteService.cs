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

        public async Task<IEnumerable<Note>> GetNotesAsync(int? topicId, int userId)
        {
            if (topicId.HasValue && topicId.Value > 0)
            {
                return await _noteRepository.GetByTopicIdAsync(topicId.Value, userId);
            }
            return await _noteRepository.GetAllAsync(userId);
        }

        public async Task<Note?> GetNoteByIdAsync(int id, int userId)
        {
            return await _noteRepository.GetByIdAsync(id, userId);
        }

        public async Task<Note> CreateNoteAsync(Note note, int userId)
        {
            note.UserId = userId;
            if (userId == 1) // Admin user
            {
                note.IsPublic = true;
            }
            return await _noteRepository.AddAsync(note);
        }

        public async Task<Note?> UpdateNoteAsync(int id, Note updatedNote, int userId)
        {
            var existingNote = await _noteRepository.GetByIdAsync(id, userId);
            if (existingNote == null) return null;

            // Enforce that only the owner can modify a note
            if (existingNote.UserId != userId)
            {
                return null;
            }

            existingNote.Title = updatedNote.Title;
            existingNote.Content = updatedNote.Content;
            existingNote.TopicId = updatedNote.TopicId;
            existingNote.IsPublic = updatedNote.IsPublic;
            existingNote.UpdatedAt = DateTime.UtcNow;

            return await _noteRepository.UpdateAsync(existingNote);
        }

        public async Task<bool> DeleteNoteAsync(int id, int userId)
        {
            return await _noteRepository.DeleteAsync(id, userId);
        }
    }
}
