import { useEffect, useState, useRef } from 'react';
import Sidebar from './components/Sidebar';
import NotesList from './components/NotesList';
import NoteEditor from './components/NoteEditor';
import FlashcardsView from './components/FlashcardsView';
import QuestionBankView from './components/QuestionBankView';
import DashboardView from './components/DashboardView';
import LoginView from './components/LoginView';
import { apiFetch, setAccessToken, setOnLogoutCallback } from './api';

interface Topic {
  id: number;
  title: string;
  description: string;
  createdAt: string;
}

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

type TabType = 'notebook' | 'flashcards' | 'questions' | 'dashboard';

function App() {
  const [activeTab, setActiveTab] = useState<TabType>('notebook');

  // Authentication State
  const [activeUser, setActiveUser] = useState<{ id: number; username: string } | null>(() => {
    const saved = sessionStorage.getItem('activeUser');
    if (saved) {
      try {
        return JSON.parse(saved);
      } catch {
        return null;
      }
    }
    return null;
  });

  // Notebook States
  const [topics, setTopics] = useState<Topic[]>([]);
  const [notes, setNotes] = useState<Note[]>([]);
  const [activeTopicId, setActiveTopicId] = useState<number | null>(null);
  const [activeNote, setActiveNote] = useState<Note | null>(null);
  
  // General Page States
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  // Spaced Repetition Session Trackers
  const [durationSeconds, setDurationSeconds] = useState(0);
  const [notesReviewedCount, setNotesReviewedCount] = useState(0);
  const [cardsReviewedCount, setCardsReviewedCount] = useState(0);
  const [questionsAttemptedCount, setQuestionsAttemptedCount] = useState(0);
  const [sessionTrigger, setSessionTrigger] = useState(0); // Triggers dashboard heatmap re-fetch
  const [secondsUntilLogout, setSecondsUntilLogout] = useState<number>(15 * 60);
  const lastActivityRef = useRef<number>(Date.now());

  const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5100';

  // 1. Session Timer Hook
  useEffect(() => {
    const timer = setInterval(() => {
      setDurationSeconds((prev) => prev + 1);
    }, 1000);
    return () => clearInterval(timer);
  }, []);

  // 2. Initial Data Load
  const fetchData = async () => {
    if (!activeUser) return;
    setLoading(true);
    setError(null);
    try {
      const topicsRes = await apiFetch(`${apiUrl}/api/topics`);
      if (!topicsRes.ok) throw new Error('Failed to fetch topics');
      const topicsData = await topicsRes.json();
      setTopics(topicsData);

      const notesRes = await apiFetch(`${apiUrl}/api/notes`);
      if (!notesRes.ok) throw new Error('Failed to fetch notes');
      const notesData = await notesRes.json();
      setNotes(notesData);

      if (notesData.length > 0) {
        setActiveNote(notesData[0]);
      }
    } catch (err: any) {
      console.error(err);
      setError(err.message || 'Failed to connect to the backend server. Please verify it is running.');
    } finally {
      setLoading(false);
    }
  };

  // Mount Session Restorer Hook
  useEffect(() => {
    const restoreSession = async () => {
      if (activeUser) {
        setLoading(true);
        try {
          const res = await fetch(`${apiUrl}/api/auth/refresh`, {
            method: 'POST',
            credentials: 'include',
          });
          if (res.ok) {
            const data = await res.json();
            setAccessToken(data.accessToken);
            setOnLogoutCallback(handleLogout);
            await fetchData();
          } else {
            handleLogout();
          }
        } catch {
          handleLogout();
        } finally {
          setLoading(false);
        }
      }
    };
    restoreSession();
  }, [activeUser]);

  const handleLogout = async () => {
    try {
      await fetch(`${apiUrl}/api/auth/logout`, {
        method: 'POST',
        credentials: 'include',
      });
    } catch (err) {
      console.error('Logout request failed on server:', err);
    }
    sessionStorage.removeItem('activeUser');
    setAccessToken(null);
    setActiveUser(null);
    setTopics([]);
    setNotes([]);
    setActiveNote(null);
    setActiveTopicId(null);
  };

  // 3. Log Study Session on Tab Change
  const logCurrentSession = async () => {
    if (!activeUser) return;
    if (durationSeconds < 5) return; // Skip logging sessions under 5 seconds to prevent spam

    const payload = {
      durationSeconds,
      notesReviewedCount,
      cardsReviewedCount,
      questionsAttemptedCount,
    };

    try {
      await apiFetch(`${apiUrl}/api/studysessions`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(payload),
      });

      // Reset session stats
      setDurationSeconds(0);
      setNotesReviewedCount(0);
      setCardsReviewedCount(0);
      setQuestionsAttemptedCount(0);
      setSessionTrigger((prev) => prev + 1); // Refresh stats on dashboard
    } catch (err) {
      console.error('Failed to log study session:', err);
    }
  };

  const handleTabChange = async (tab: TabType) => {
    await logCurrentSession();
    setActiveTab(tab);
  };

  // 4. Inactivity Idle Timeout Hook (Option C - 15 minutes with countdown timer based on absolute time differences)
  useEffect(() => {
    if (!activeUser) return;

    const resetInactivity = () => {
      lastActivityRef.current = Date.now();
      setSecondsUntilLogout(15 * 60);
    };

    const events = ['mousemove', 'keydown', 'mousedown', 'scroll', 'click'];
    events.forEach((event) => {
      window.addEventListener(event, resetInactivity);
    });

    return () => {
      events.forEach((event) => {
        window.removeEventListener(event, resetInactivity);
      });
    };
  }, [activeUser]);

  useEffect(() => {
    if (!activeUser) return;

    const interval = setInterval(() => {
      const elapsedMs = Date.now() - lastActivityRef.current;
      const remainingSeconds = Math.max(0, Math.floor((15 * 60 * 1000 - elapsedMs) / 1000));
      
      setSecondsUntilLogout(remainingSeconds);

      if (remainingSeconds <= 0) {
        clearInterval(interval);
        console.log("Inactivity timeout reached. Logging out...");
        handleLogout();
      }
    }, 1000);

    return () => clearInterval(interval);
  }, [activeUser]);

  // 4. Notebook CRUD Handlers
  const displayedNotes = activeTopicId
    ? notes.filter((note) => note.topicId === activeTopicId)
    : notes;

  const handleSelectTopic = (topicId: number | null) => {
    setActiveTopicId(topicId);
    const filtered = topicId ? notes.filter((n) => n.topicId === topicId) : notes;
    setActiveNote(filtered.length > 0 ? filtered[0] : null);
  };

  const handleCreateNote = async () => {
    if (!activeUser) return;
    const targetTopicId = activeTopicId || (topics.length > 0 ? topics[0].id : 1);
    const newNotePayload = {
      title: 'Untitled Note',
      content: '<p>Start writing your thoughts...</p>',
      topicId: targetTopicId,
    };

    try {
      const res = await apiFetch(`${apiUrl}/api/notes`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(newNotePayload),
      });

      if (!res.ok) throw new Error('Failed to create note');
      const createdNote = await res.json();
      
      setNotes((prev) => [createdNote, ...prev]);
      setActiveNote(createdNote);

      if (activeTopicId && activeTopicId !== targetTopicId) {
        setActiveTopicId(targetTopicId);
      }
    } catch (err: any) {
      alert(`Error: ${err.message}`);
    }
  };

  const handleSaveNote = async (id: number, title: string, content: string, isPublic?: boolean) => {
    if (!activeUser) return;
    const originalNote = notes.find((n) => n.id === id);
    if (!originalNote) return;

    const updatedPayload = {
      ...originalNote,
      title,
      content,
      isPublic: isPublic !== undefined ? isPublic : originalNote.isPublic,
    };

    try {
      const res = await apiFetch(`${apiUrl}/api/notes/${id}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(updatedPayload),
      });

      if (!res.ok) throw new Error('Failed to save note');
      const savedNote = await res.json();

      setNotes((prev) => prev.map((n) => (n.id === id ? savedNote : n)));
      if (activeNote && activeNote.id === id) {
        setActiveNote(savedNote);
      }
      
      // Log note review count increment
      setNotesReviewedCount((prev) => prev + 1);
    } catch (err: any) {
      alert(`Error: ${err.message}`);
    }
  };

  const handleDeleteNote = async (id: number) => {
    if (!activeUser) return;
    try {
      const res = await apiFetch(`${apiUrl}/api/notes/${id}`, {
        method: 'DELETE',
      });

      if (!res.ok) throw new Error('Failed to delete note');
      setNotes((prev) => prev.filter((n) => n.id !== id));

      if (activeNote && activeNote.id === id) {
        const remaining = notes.filter((n) => n.id !== id);
        const filtered = activeTopicId ? remaining.filter((n) => n.topicId === activeTopicId) : remaining;
        setActiveNote(filtered.length > 0 ? filtered[0] : null);
      }
    } catch (err: any) {
      alert(`Error: ${err.message}`);
    }
  };

  if (!activeUser) {
    return (
      <LoginView
        apiUrl={apiUrl}
        onLoginSuccess={(user) => {
          setAccessToken(user.accessToken);
          setOnLogoutCallback(handleLogout);
          setActiveUser({ id: user.id, username: user.username });
          sessionStorage.setItem('activeUser', JSON.stringify({ id: user.id, username: user.username }));
        }}
      />
    );
  }

  if (loading) {
    return (
      <div className="loader-container">
        <div className="spinner"></div>
        <p style={{ color: 'var(--text-secondary)' }}>Opening your Notion-style workspace...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="error-container">
        <h2 className="error-title">Workspace Connection Error</h2>
        <p className="error-msg">{error}</p>
        <div style={{ display: 'flex', gap: '1rem', marginTop: '1.5rem' }}>
          <button className="btn-retry" onClick={fetchData}>
            Reconnect
          </button>
          <button 
            className="btn-retry" 
            style={{ background: 'rgba(239, 68, 68, 0.15)', border: '1px solid rgba(239, 68, 68, 0.3)', color: '#ff7675' }} 
            onClick={handleLogout}
          >
            Reset Session
          </button>
        </div>
      </div>
    );
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100vh', width: '100vw' }}>
      
      {/* Top Navigation Bar */}
      <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '0.75rem 2rem', background: 'var(--bg-sidebar)', borderBottom: '1px solid var(--border-color)', height: '56px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <div className="sidebar-logo">💡</div>
          <span style={{ fontWeight: '600', fontSize: '1rem', letterSpacing: '-0.01em', background: 'var(--accent-gradient)', WebkitBackgroundClip: 'text', WebkitTextFillColor: 'transparent', backgroundClip: 'text' }}>
            TopicPortal Hub
          </span>
        </div>
        
        {/* Navigation Tabs */}
        <nav style={{ display: 'flex', gap: '0.5rem' }}>
          <button
            onClick={() => handleTabChange('notebook')}
            className={`toolbar-btn ${activeTab === 'notebook' ? 'active' : ''}`}
            style={{ fontSize: '0.85rem', padding: '0.4rem 0.8rem' }}
          >
            📓 Notebook
          </button>
          <button
            onClick={() => handleTabChange('flashcards')}
            className={`toolbar-btn ${activeTab === 'flashcards' ? 'active' : ''}`}
            style={{ fontSize: '0.85rem', padding: '0.4rem 0.8rem' }}
          >
            🎴 Flashcards
          </button>
          <button
            onClick={() => handleTabChange('questions')}
            className={`toolbar-btn ${activeTab === 'questions' ? 'active' : ''}`}
            style={{ fontSize: '0.85rem', padding: '0.4rem 0.8rem' }}
          >
            🎯 Practice
          </button>
          <button
            onClick={() => handleTabChange('dashboard')}
            className={`toolbar-btn ${activeTab === 'dashboard' ? 'active' : ''}`}
            style={{ fontSize: '0.85rem', padding: '0.4rem 0.8rem' }}
          >
            📊 Dashboard
          </button>
        </nav>

        {/* User Info & Live Timer */}
        <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', display: 'flex', alignItems: 'center', gap: '1.25rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
            <span style={{ width: '6px', height: '6px', borderRadius: '50%', background: 'var(--accent-cyan)' }}></span>
            Study Timer: {Math.floor(durationSeconds / 60)}m {durationSeconds % 60}s
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', borderLeft: '1px solid var(--border-color)', paddingLeft: '1.25rem' }}>
            <span style={{ width: '6px', height: '6px', borderRadius: '50%', background: secondsUntilLogout < 60 ? '#ff7675' : '#f39c12' }}></span>
            Auto-logout in: {Math.floor(secondsUntilLogout / 60)}m {secondsUntilLogout % 60}s
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', borderLeft: '1px solid var(--border-color)', paddingLeft: '1.25rem' }}>
            <span style={{ color: 'var(--text-primary)', fontWeight: '500' }}>👤 {activeUser.username}</span>
            <button
              onClick={handleLogout}
              className="toolbar-btn"
              style={{ fontSize: '0.75rem', padding: '0.25rem 0.5rem', background: 'rgba(239, 68, 68, 0.15)', border: '1px solid rgba(239, 68, 68, 0.3)', color: '#ff7675', cursor: 'pointer', borderRadius: '4px' }}
            >
              Logout
            </button>
          </div>
        </div>
      </header>

      {/* Main Workspace Panels depending on Tab */}
      <main style={{ flexGrow: 1, overflow: 'hidden', display: 'flex', width: '100%' }}>
        
        {/* Tab 1: Notion Notes */}
        {activeTab === 'notebook' && (
          <div className="workspace-container" style={{ height: '100%', width: '100%' }}>
            <Sidebar
              topics={topics}
              activeTopicId={activeTopicId}
              onSelectTopic={handleSelectTopic}
              notes={notes}
            />
            <NotesList
              notes={displayedNotes}
              activeNoteId={activeNote ? activeNote.id : null}
              onSelectNote={setActiveNote}
              onDeleteNote={handleDeleteNote}
              onCreateNote={handleCreateNote}
              activeTopicId={activeTopicId}
              topics={topics}
              activeUserId={activeUser.id}
            />
            <NoteEditor 
              note={activeNote} 
              activeUserId={activeUser.id}
              onSave={handleSaveNote} 
            />
          </div>
        )}

        {/* Tab 2: Spaced Repetition Cards */}
        {activeTab === 'flashcards' && (
          <div style={{ height: '100%', width: '100%', overflow: 'hidden' }}>
            <FlashcardsView
              topics={topics}
              apiUrl={apiUrl}
              onCardReviewed={() => setCardsReviewedCount((prev) => prev + 1)}
            />
          </div>
        )}

        {/* Tab 3: Practice Quiz bank */}
        {activeTab === 'questions' && (
          <div style={{ height: '100%', width: '100%', overflow: 'hidden' }}>
            <QuestionBankView
              topics={topics}
              apiUrl={apiUrl}
              onQuestionAttempted={() => setQuestionsAttemptedCount((prev) => prev + 1)}
            />
          </div>
        )}

        {/* Tab 4: Analytics Stats Page */}
        {activeTab === 'dashboard' && (
          <div style={{ height: '100%', width: '100%', overflowY: 'auto' }}>
            <DashboardView
              topics={topics}
              apiUrl={apiUrl}
              sessionTrigger={sessionTrigger}
            />
          </div>
        )}
      </main>
    </div>
  );
}

export default App;
