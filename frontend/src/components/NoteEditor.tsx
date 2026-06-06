import React, { useEffect, useState } from 'react';
import { useEditor, EditorContent } from '@tiptap/react';
import StarterKit from '@tiptap/starter-kit';

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

interface NoteEditorProps {
  note: Note | null;
  activeUserId: number;
  onSave: (id: number, title: string, content: string, isPublic?: boolean) => Promise<void>;
}

export const NoteEditor: React.FC<NoteEditorProps> = ({ note, activeUserId, onSave }) => {
  const [title, setTitle] = useState('');
  const [contentHtml, setContentHtml] = useState('');
  const [isPublic, setIsPublic] = useState(false);
  const [saveStatus, setSaveStatus] = useState<'idle' | 'saving' | 'saved'>('saved');

  // Initialize TipTap Editor
  const editor = useEditor({
    extensions: [
      StarterKit.configure({
        // Disable default heading 1 for Notion style (we use H2/H3 for documents)
        heading: {
          levels: [2, 3],
        },
      }),
    ],
    content: '',
    onUpdate: ({ editor }) => {
      setContentHtml(editor.getHTML());
    },
  });

  // Sync editor when active note changes
  useEffect(() => {
    if (editor && note) {
      setTitle(note.title);
      setContentHtml(note.content);
      setIsPublic(note.isPublic || false);
      
      const isOwner = note.userId === activeUserId;
      editor.setEditable(isOwner);
      
      // Prevent infinite cursor resets by checking if content changed
      if (editor.getHTML() !== note.content) {
        editor.commands.setContent(note.content);
      }
      setSaveStatus('saved');
    }
  }, [note?.id, editor, activeUserId]);

  // Debounced auto-save effect
  useEffect(() => {
    if (!note) return;
    const isOwner = note.userId === activeUserId;
    if (!isOwner) return;

    // Check if the current state differs from the saved database note
    const isChanged = title !== note.title || contentHtml !== note.content || isPublic !== note.isPublic;
    if (!isChanged) return;

    // When the user starts typing, status turns to idle
    setSaveStatus('idle');

    const delayDebounceFn = setTimeout(async () => {
      setSaveStatus('saving');
      try {
        await onSave(note.id, title, contentHtml, isPublic);
        setSaveStatus('saved');
      } catch (err) {
        console.error('Auto-save failed:', err);
        setSaveStatus('idle');
      }
    }, 1500); // 1.5 seconds debounce

    return () => clearTimeout(delayDebounceFn);
  }, [title, contentHtml, isPublic, note?.id, activeUserId]);

  if (!note || !editor) {
    return (
      <div className="editor-panel">
        <div className="empty-workspace-state">
          <div className="empty-state-icon">📝</div>
          <h2>No Note Selected</h2>
          <p style={{ marginTop: '0.5rem', fontSize: '0.9rem' }}>
            Choose a note from the list or click "+ New Note" to start writing.
          </p>
        </div>
      </div>
    );
  }

  const isOwner = note.userId === activeUserId;

  return (
    <div className="editor-panel">
      {/* Editor Status Bar */}
      <div className="editor-header-bar">
        <div className="editor-status-text">
          {isOwner ? (
            <>
              <span className={`editor-status-dot ${saveStatus}`}></span>
              {saveStatus === 'saved' && 'All changes saved'}
              {saveStatus === 'saving' && 'Saving...'}
              {saveStatus === 'idle' && 'Unsaved changes'}
            </>
          ) : (
            <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>👁️ Read-Only Mode</span>
          )}
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
          {isOwner ? (
            <>
              <label style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', fontSize: '0.85rem', cursor: 'pointer', color: 'var(--text-secondary)' }}>
                <input
                  type="checkbox"
                  checked={isPublic}
                  onChange={(e) => setIsPublic(e.target.checked)}
                  style={{ cursor: 'pointer' }}
                />
                🌐 Public
              </label>
              <button 
                className="btn-new-note" 
                style={{ padding: '0.3rem 0.6rem', background: 'var(--bg-card)', border: '1px solid var(--border-color)', color: 'var(--text-secondary)' }}
                onClick={() => onSave(note.id, title, editor.getHTML(), isPublic).then(() => setSaveStatus('saved'))}
              >
                Save Now
              </button>
            </>
          ) : (
            <span style={{ fontSize: '0.8rem', color: 'var(--accent-cyan)', background: 'rgba(0, 206, 201, 0.1)', padding: '0.25rem 0.5rem', borderRadius: '4px', border: '1px solid rgba(0, 206, 201, 0.2)' }}>
              🌐 Shared Publicly
            </span>
          )}
        </div>
      </div>

      <div className="editor-scrollable">
        <div className="editor-workspace">
          {/* Transparent Title Input */}
          <input
            type="text"
            className="editor-title-input"
            placeholder="Untitled Note"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            readOnly={!isOwner}
          />

          {/* TipTap Rich Text Toolbar (Only visible to owner) */}
          {isOwner && (
            <div className="editor-toolbar">
            <button
              onClick={() => editor.chain().focus().toggleBold().run()}
              className={`toolbar-btn ${editor.isActive('bold') ? 'active' : ''}`}
              title="Bold"
            >
              <strong>B</strong>
            </button>
            <button
              onClick={() => editor.chain().focus().toggleItalic().run()}
              className={`toolbar-btn ${editor.isActive('italic') ? 'active' : ''}`}
              title="Italic"
            >
              <em>I</em>
            </button>
            <button
              onClick={() => editor.chain().focus().toggleStrike().run()}
              className={`toolbar-btn ${editor.isActive('strike') ? 'active' : ''}`}
              title="Strike"
            >
              <s>S</s>
            </button>
            
            <div className="toolbar-divider" />

            <button
              onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}
              className={`toolbar-btn ${editor.isActive('heading', { level: 2 }) ? 'active' : ''}`}
              title="Heading 2"
            >
              H2
            </button>
            <button
              onClick={() => editor.chain().focus().toggleHeading({ level: 3 }).run()}
              className={`toolbar-btn ${editor.isActive('heading', { level: 3 }) ? 'active' : ''}`}
              title="Heading 3"
            >
              H3
            </button>
            
            <div className="toolbar-divider" />

            <button
              onClick={() => editor.chain().focus().toggleBulletList().run()}
              className={`toolbar-btn ${editor.isActive('bulletList') ? 'active' : ''}`}
              title="Bullet List"
            >
              • List
            </button>
            <button
              onClick={() => editor.chain().focus().toggleOrderedList().run()}
              className={`toolbar-btn ${editor.isActive('orderedList') ? 'active' : ''}`}
              title="Numbered List"
            >
              1. List
            </button>
            <button
              onClick={() => editor.chain().focus().toggleBlockquote().run()}
              className={`toolbar-btn ${editor.isActive('blockquote') ? 'active' : ''}`}
              title="Blockquote"
            >
              “ Quote
            </button>
            <button
              onClick={() => editor.chain().focus().toggleCodeBlock().run()}
              className={`toolbar-btn ${editor.isActive('codeBlock') ? 'active' : ''}`}
              title="Code Block"
            >
              &lt;/&gt; Code
            </button>

            <div className="toolbar-divider" />

            <button
              onClick={() => editor.chain().focus().undo().run()}
              className="toolbar-btn"
              title="Undo"
              disabled={!editor.can().undo()}
            >
              ↶
            </button>
            <button
              onClick={() => editor.chain().focus().redo().run()}
              className="toolbar-btn"
              title="Redo"
              disabled={!editor.can().redo()}
            >
              ↷
            </button>
          </div>
          )}

          {/* TipTap WYSIWYG Content Area */}
          <div className="tiptap-container">
            <EditorContent editor={editor} />
          </div>
        </div>
      </div>
    </div>
  );
};
export default NoteEditor;
