import React, { useState } from 'react';

interface Note {
  id: number;
  title: string;
  content: string;
  topicId: number;
  userId: number;
  isPublic: boolean;
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
  activeUserId: number;
}

export const NotesList: React.FC<NotesListProps> = ({
  notes,
  activeNoteId,
  onSelectNote,
  onDeleteNote,
  onCreateNote,
  activeTopicId,
  topics,
  activeUserId,
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

  const getConceptRank = (title: string): number => {
    const t = title.toLowerCase();
    
    // OOP Pillars (10-13)
    if (t.includes('encapsulation')) return 10;
    if (t.includes('inheritance')) return 11;
    if (t.includes('polymorphism')) return 12;
    if (t.includes('abstraction')) return 13;
    
    // SOLID Principles (20-24)
    if (t.includes('single responsibility') || t.includes('srp') || t.startsWith('s -') || t.startsWith('s —') || t.startsWith('s:')) return 20;
    if (t.includes('open/closed') || t.includes('open closed') || t.includes('ocp') || t.startsWith('o -') || t.startsWith('o —') || t.startsWith('o:')) return 21;
    if (t.includes('liskov') || t.includes('lsp') || t.startsWith('l -') || t.startsWith('l —') || t.startsWith('l:')) return 22;
    if (t.includes('interface segregation') || t.includes('isp') || t.startsWith('i -') || t.startsWith('i —') || t.startsWith('i:')) return 23;
    if (t.includes('dependency inversion') || t.includes('dip') || t.startsWith('d -') || t.startsWith('d —') || t.startsWith('d:')) return 24;
    
    // ACID Properties (30-33)
    if (t.includes('atomicity')) return 30;
    if (t.includes('consistency') && !t.includes('liskov') && !t.includes('cap')) return 31;
    if (t.includes('isolation')) return 32;
    if (t.includes('durability')) return 33;
    
    // SQL Joins (40-45)
    if (t.includes('inner join')) return 40;
    if (t.includes('left join')) return 41;
    if (t.includes('right join')) return 42;
    if (t.includes('full outer') || t.includes('full join')) return 43;
    if (t.includes('cross join')) return 44;
    if (t.includes('self join')) return 45;
    
    // REST API Verbs (50-54)
    if (t.startsWith('get ') || t.startsWith('get:') || t.includes('get method') || t.includes('get request')) return 50;
    if (t.startsWith('post ') || t.startsWith('post:') || t.includes('post method') || t.includes('post request')) return 51;
    if (t.startsWith('put ') || t.startsWith('put:') || t.includes('put method') || t.includes('put request')) return 52;
    if (t.startsWith('patch ') || t.startsWith('patch:') || t.includes('patch method') || t.includes('patch request')) return 53;
    if (t.startsWith('delete ') || t.startsWith('delete:') || t.includes('delete method') || t.includes('delete request')) return 54;
    
    // Transaction Isolation Levels (60-63)
    if (t.includes('read uncommitted')) return 60;
    if (t.includes('read committed')) return 61;
    if (t.includes('repeatable read')) return 62;
    if (t.includes('serializable')) return 63;
    
    // Sorting Algorithms (70-75)
    if (t.includes('bubble sort')) return 70;
    if (t.includes('selection sort')) return 71;
    if (t.includes('insertion sort')) return 72;
    if (t.includes('merge sort')) return 73;
    if (t.includes('quick sort')) return 74;
    if (t.includes('heap sort')) return 75;

    // Web Security (80-83)
    if (t.includes('sql injection') || t.includes('sqli')) return 80;
    if (t.includes('xss') || t.includes('cross-site scripting') || t.includes('cross site scripting')) return 81;
    if (t.includes('csrf') || t.includes('cross-site request forgery') || t.includes('cross site request forgery')) return 82;
    if (t.includes('idor') || t.includes('insecure direct object')) return 83;

    // CAP Theorem (90-92)
    if (t.includes('cap consistency') || (t.includes('consistency') && t.includes('cap'))) return 90;
    if (t.includes('availability') && t.includes('cap')) return 91;
    if (t.includes('partition tolerance') || (t.includes('partition') && t.includes('cap'))) return 92;

    return 999;
  };

  const sortedNotes = [...filteredNotes].sort((a, b) => {
    const rankA = getConceptRank(a.title);
    const rankB = getConceptRank(b.title);
    if (rankA !== rankB) {
      return rankA - rankB;
    }
    return new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime();
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
        {sortedNotes.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '2rem 0', color: 'var(--text-muted)', fontSize: '0.9rem' }}>
            No notes found
          </div>
        ) : (
          sortedNotes.map((note) => (
            <div
              key={note.id}
              className={`note-item-card ${activeNoteId === note.id ? 'active' : ''}`}
              onClick={() => onSelectNote(note)}
            >
              <div className="note-item-header">
                <span className="note-item-title" title={note.title} style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', overflow: 'hidden' }}>
                  {note.isPublic && <span title="Public Note">🌐</span>}
                  <span style={{ textOverflow: 'ellipsis', overflow: 'hidden', whiteSpace: 'nowrap' }}>
                    {note.title || 'Untitled Note'}
                  </span>
                </span>
                {note.userId === activeUserId && (
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
                )}
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
