import React, { useState } from 'react';

interface Note {
  id: number;
  title: string;
  content: string;
  topicId: number;
  createdAt: string;
  updatedAt: string;
}

interface Topic {
  id: number;
  title: string;
}

interface NotesListProps {
  notes: Note[];
  activeNoteId: number | null;
  onSelectNote: (note: Note) => void;
  onDeleteNote: (id: number) => void;
  onCreateNote: () => void;
  activeTopicId: number | null;
  topics: Topic[];
}

export const NotesList: React.FC<NotesListProps> = ({
  notes,
  activeNoteId,
  onSelectNote,
  onDeleteNote,
  onCreateNote,
  activeTopicId,
  topics,
}) => {
  const [searchQuery, setSearchQuery] = useState('');

  // Find selected topic title
  const activeTopic = topics.find((t) => t.id === activeTopicId);
  const topicTitle = activeTopic ? activeTopic.title : 'All Notes';

  // Strip HTML tags for clean card descriptions
  const getExcerpt = (html: string) => {
    if (!html) return 'No content yet...';
    const text = html.replace(/<[^>]*>?/gm, ' ').trim();
    return text.length > 80 ? text.substring(0, 80) + '...' : text;
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  // Filter notes based on local search query
  const filteredNotes = notes.filter((note) => {
    const titleMatch = note.title.toLowerCase().includes(searchQuery.toLowerCase());
    const contentMatch = note.content.toLowerCase().includes(searchQuery.toLowerCase());
    return titleMatch || contentMatch;
  });

  return (
    <div className="notes-list-panel">
      <div className="notes-list-header">
        <div className="notes-list-title-row">
          <span className="notes-list-title">{topicTitle}</span>
          <button className="btn-new-note" onClick={onCreateNote}>
            <span>+</span> New Note
          </button>
        </div>
        <input
          type="text"
          className="notes-search-input"
          placeholder="Search notes..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
        />
      </div>

      <div className="notes-list-scrollable">
        {filteredNotes.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '2rem 0', color: 'var(--text-muted)', fontSize: '0.9rem' }}>
            No notes found
          </div>
        ) : (
          filteredNotes.map((note) => (
            <div
              key={note.id}
              className={`note-item-card ${activeNoteId === note.id ? 'active' : ''}`}
              onClick={() => onSelectNote(note)}
            >
              <div className="note-item-header">
                <span className="note-item-title" title={note.title}>
                  {note.title || 'Untitled Note'}
                </span>
                <button
                  className="btn-delete-note"
                  onClick={(e) => {
                    e.stopPropagation(); // Avoid triggering selection
                    if (confirm('Are you sure you want to delete this note?')) {
                      onDeleteNote(note.id);
                    }
                  }}
                  title="Delete Note"
                >
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  >
                    <polyline points="3 6 5 6 21 6"></polyline>
                    <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
                    <line x1="10" y1="11" x2="10" y2="17"></line>
                    <line x1="14" y1="11" x2="14" y2="17"></line>
                  </svg>
                </button>
              </div>
              <p className="note-item-excerpt">{getExcerpt(note.content)}</p>
              <div className="note-item-footer">
                <span>{formatDate(note.updatedAt)}</span>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
};
export default NotesList;
