import React, { useState, useEffect } from 'react';
import apiFetch from '../api';

interface Flashcard {
  id: number;
  front: string;
  back: string;
  topicId: number;
  nextReviewDate: string;
  intervalDays: number;
}

interface Topic {
  id: number;
  title: string;
}

interface FlashcardsViewProps {
  topics: Topic[];
  apiUrl: string;
  onCardReviewed: () => void; // Call parent to track study stats
}

export const FlashcardsView: React.FC<FlashcardsViewProps> = ({
  topics,
  apiUrl,
  onCardReviewed,
}) => {
  const [cards, setCards] = useState<Flashcard[]>([]);
  const [loading, setLoading] = useState(true);
  const [currentIdx, setCurrentIdx] = useState(0);
  const [isFlipped, setIsFlipped] = useState(false);
  const [reviewMode, setReviewMode] = useState<'due' | 'all'>('due');
  const [activeTopicFilter, setActiveTopicFilter] = useState<number | null>(null);
  const [orderByFilter, setOrderByFilter] = useState<'default' | 'alphabetical' | 'newest' | 'oldest'>('default');
  
  // Create Card Form State
  const [showAddForm, setShowAddForm] = useState(false);
  const [newFront, setNewFront] = useState('');
  const [newBack, setNewBack] = useState('');
  const [newTopicId, setNewTopicId] = useState(topics.length > 0 ? topics[0].id : 1);

  // Synchronize newTopicId when topics load asynchronously
  useEffect(() => {
    if (topics.length > 0) {
      setNewTopicId(topics[0].id);
    }
  }, [topics]);

  const fetchCards = async () => {
    setLoading(true);
    try {
      const res = await apiFetch(`${apiUrl}/api/flashcards`);
      if (!res.ok) throw new Error('Failed to load flashcards');
      const data: Flashcard[] = await res.json();
      setCards(data);
      setCurrentIdx(0);
      setIsFlipped(false);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchCards();
  }, []);

  // Filter cards based on review mode and topic
  const filteredCards = cards.filter(c => {
    const matchesReviewMode = reviewMode === 'due'
      ? new Date(c.nextReviewDate) <= new Date()
      : true;
    const matchesTopic = !activeTopicFilter || c.topicId === activeTopicFilter;
    return matchesReviewMode && matchesTopic;
  });

  // Sort cards based on orderByFilter
  const sortedCards = [...filteredCards].sort((a, b) => {
    if (orderByFilter === 'alphabetical') {
      return a.front.localeCompare(b.front);
    }
    if (orderByFilter === 'newest') {
      return b.id - a.id;
    }
    if (orderByFilter === 'oldest') {
      return a.id - b.id;
    }
    // 'default': scheduled nextReviewDate first, then creation id
    const dateA = new Date(a.nextReviewDate).getTime();
    const dateB = new Date(b.nextReviewDate).getTime();
    if (dateA !== dateB) {
      return dateA - dateB;
    }
    return a.id - b.id;
  });

  const displayedCards = sortedCards;

  // Reset index when filters or order changes
  useEffect(() => {
    setCurrentIdx(0);
    setIsFlipped(false);
  }, [activeTopicFilter, orderByFilter, reviewMode]);

  const handleFlip = () => {
    setIsFlipped(!isFlipped);
  };

  const handleReview = async (quality: 'easy' | 'hard') => {
    if (displayedCards.length === 0) return;
    const currentCard = displayedCards[currentIdx];

    try {
      const res = await apiFetch(`${apiUrl}/api/flashcards/${currentCard.id}/review?quality=${quality}`, {
        method: 'POST',
      });
      if (!res.ok) throw new Error('Failed to review card');
      const updatedCard = await res.json();

      // Update local state card array
      setCards(prev => prev.map(c => c.id === updatedCard.id ? updatedCard : c));
      
      // Notify parent study session log
      onCardReviewed();

      // Animate next card transition
      setIsFlipped(false);
      setTimeout(() => {
        if (currentIdx < displayedCards.length - 1) {
          setCurrentIdx(prev => prev + 1);
        } else {
          // Finished the deck! Reset index
          setCurrentIdx(0);
        }
      }, 300);
    } catch (err) {
      console.error(err);
    }
  };

  const handleCreateCard = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newFront || !newBack) return;

    const payload = {
      front: newFront,
      back: newBack,
      topicId: newTopicId,
    };

    try {
      const res = await apiFetch(`${apiUrl}/api/flashcards`, {
        method: 'POST',
        headers: { 
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(payload),
      });

      if (!res.ok) throw new Error('Failed to create flashcard');
      const createdCard = await res.json();

      setCards(prev => [createdCard, ...prev]);
      setNewFront('');
      setNewBack('');
      setShowAddForm(false);
      alert('Flashcard created successfully!');
    } catch (err: any) {
      alert(`Error: ${err.message}`);
    }
  };

  if (loading) {
    return (
      <div className="loader-container">
        <div className="spinner"></div>
        <p style={{ color: 'var(--text-secondary)' }}>Gathering your study cards...</p>
      </div>
    );
  }

  const activeCard = displayedCards[currentIdx];

  return (
    <div className="study-area" style={{ height: '100%', overflowY: 'auto', justifyContent: 'flex-start' }}>
      {/* Review Mode Selector */}
      <div style={{ display: 'flex', justifyContent: 'space-between', width: '100%', maxWidth: '520px', alignItems: 'center', marginBottom: '1rem' }}>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button
            className={`btn-status-toggle ${reviewMode === 'due' ? 'confident' : 'unseen'}`}
            onClick={() => { setReviewMode('due'); }}
            style={{ borderRadius: '20px' }}
          >
            Due Today ({cards.filter(c => new Date(c.nextReviewDate) <= new Date()).length})
          </button>
          <button
            className={`btn-status-toggle ${reviewMode === 'all' ? 'confident' : 'unseen'}`}
            onClick={() => { setReviewMode('all'); }}
            style={{ borderRadius: '20px' }}
          >
            All Cards ({cards.length})
          </button>
        </div>
        <button
          className="btn-new-note"
          onClick={() => setShowAddForm(!showAddForm)}
          style={{ padding: '0.4rem 0.8rem', fontSize: '0.8rem' }}
        >
          {showAddForm ? 'Cancel' : '+ Add Card'}
        </button>
      </div>

      {/* Filters and sorting header */}
      <div className="dashboard-panel-box" style={{ width: '100%', maxWidth: '520px', marginBottom: '1.5rem', padding: '1rem' }}>
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.75rem', alignItems: 'center' }}>
          {/* Topic Select */}
          <div style={{ flex: '1 1 180px' }}>
            <label style={{ display: 'block', fontSize: '0.7rem', color: 'var(--text-secondary)', marginBottom: '0.2rem' }}>Topic</label>
            <select
              className="notes-search-input"
              value={activeTopicFilter || ''}
              onChange={e => setActiveTopicFilter(e.target.value ? Number(e.target.value) : null)}
              style={{ background: 'var(--bg-darker)', color: 'var(--text-primary)', padding: '0.35rem 0.5rem', height: '32px', fontSize: '0.8rem' }}
            >
              <option value="">All Topics</option>
              {topics.map(t => (
                <option key={t.id} value={t.id}>{t.title}</option>
              ))}
            </select>
          </div>

          {/* Sort By Select */}
          <div style={{ flex: '1 1 150px' }}>
            <label style={{ display: 'block', fontSize: '0.7rem', color: 'var(--text-secondary)', marginBottom: '0.2rem' }}>Sort By</label>
            <select
              className="notes-search-input"
              value={orderByFilter}
              onChange={e => setOrderByFilter(e.target.value as any)}
              style={{ background: 'var(--bg-darker)', color: 'var(--text-primary)', padding: '0.35rem 0.5rem', height: '32px', fontSize: '0.8rem' }}
            >
              <option value="default">Due First</option>
              <option value="alphabetical">A-Z (Question)</option>
              <option value="newest">Newest First</option>
              <option value="oldest">Oldest First</option>
            </select>
          </div>
        </div>
      </div>

      {/* Add Card Form Box */}
      {showAddForm && (
        <form onSubmit={handleCreateCard} className="dashboard-panel-box" style={{ width: '100%', maxWidth: '520px', marginBottom: '2rem' }}>
          <h3 className="dashboard-panel-title">Create New Flashcard</h3>
          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', fontSize: '0.8rem', color: 'var(--text-secondary)', marginBottom: '0.35rem' }}>Front / Question</label>
            <textarea
              className="notes-search-input"
              rows={2}
              style={{ width: '100%', resize: 'none', height: 'auto', padding: '0.5rem' }}
              placeholder="e.g. What is the Big O of Binary Search?"
              value={newFront}
              onChange={e => setNewFront(e.target.value)}
              required
            />
          </div>
          <div style={{ marginBottom: '1.25rem' }}>
            <label style={{ display: 'block', fontSize: '0.8rem', color: 'var(--text-secondary)', marginBottom: '0.35rem' }}>Back / Answer</label>
            <textarea
              className="notes-search-input"
              rows={3}
              style={{ width: '100%', resize: 'none', height: 'auto', padding: '0.5rem' }}
              placeholder="e.g. O(log n). Since it cuts the search space in half each step."
              value={newBack}
              onChange={e => setNewBack(e.target.value)}
              required
            />
          </div>
          <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
            <div style={{ flexGrow: 1 }}>
              <label style={{ display: 'block', fontSize: '0.8rem', color: 'var(--text-secondary)', marginBottom: '0.35rem' }}>Topic</label>
              <select
                className="notes-search-input"
                value={newTopicId}
                onChange={e => setNewTopicId(Number(e.target.value))}
                style={{ background: 'var(--bg-darker)', color: 'var(--text-primary)' }}
              >
                {topics.map(t => (
                  <option key={t.id} value={t.id}>{t.title}</option>
                ))}
              </select>
            </div>
            <button type="submit" className="btn-new-note" style={{ marginTop: '1.25rem' }}>Save Card</button>
          </div>
        </form>
      )}

      {/* Main Reviewer */}
      {displayedCards.length === 0 ? (
        <div className="error-container" style={{ width: '100%', maxWidth: '520px', borderColor: 'var(--border-color)', margin: '4rem 0' }}>
          <h2 className="error-title" style={{ color: 'var(--accent-cyan)' }}>Deck Complete! 🎉</h2>
          <p className="error-msg">
            {reviewMode === 'due' 
              ? "All caught up on scheduled reviews! Check back later or toggle 'All Cards' above to practice."
              : "No flashcards in your deck yet. Click '+ Add Card' to create some!"}
          </p>
          {reviewMode === 'due' && cards.length > 0 && (
            <button className="btn-retry" onClick={() => { setReviewMode('all'); }}>
              Study Ahead
            </button>
          )}
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', width: '100%' }}>
          <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
            Card {currentIdx + 1} of {displayedCards.length}
          </span>

          {/* 3D Flip Card Container */}
          <div
            className={`flashcard-container ${isFlipped ? 'flipped' : ''}`}
            onClick={handleFlip}
          >
            <div className="flashcard-inner">
              {/* Front side */}
              <div className="flashcard-front">
                <span className="card-side-label">
                  Front • {topics.find(t => t.id === activeCard?.topicId)?.title || 'General'}
                </span>
                <p className="flashcard-text">{activeCard?.front}</p>
                <span style={{ position: 'absolute', bottom: '1rem', fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                  Click to Flip
                </span>
              </div>
              {/* Back side */}
              <div className="flashcard-back">
                <span className="card-side-label">
                  Back • {topics.find(t => t.id === activeCard?.topicId)?.title || 'General'}
                </span>
                <p className="flashcard-text" style={{ whiteSpace: 'pre-line' }}>{activeCard?.back}</p>
                <span style={{ position: 'absolute', bottom: '1rem', fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                  Click to Flip Back
                </span>
              </div>
            </div>
          </div>

          {/* Action Buttons */}
          <div style={{ display: 'flex', gap: '0.75rem', width: '100%', maxWidth: '380px', marginTop: '1rem' }}>
            {!isFlipped ? (
              <button
                className="btn-login"
                onClick={handleFlip}
                style={{
                  width: '100%',
                  margin: 0,
                  padding: '0.6rem 1rem',
                  fontSize: '0.9rem',
                  background: 'var(--accent-gradient)',
                  boxShadow: '0 4px 15px rgba(6, 182, 212, 0.25)',
                }}
              >
                👀 Show Answer
              </button>
            ) : (
              <>
                <button
                  className="btn-retry"
                  onClick={() => handleReview('hard')}
                  style={{
                    flex: 1,
                    background: 'rgba(239, 68, 68, 0.15)',
                    color: 'rgb(248, 113, 113)',
                    border: '1px solid rgba(239, 68, 68, 0.3)',
                    boxShadow: 'none',
                    padding: '0.6rem',
                    fontSize: '0.85rem',
                    margin: 0
                  }}
                >
                  🔴 Hard (1d)
                </button>
                <button
                  className="btn-retry"
                  onClick={() => handleReview('easy')}
                  style={{
                    flex: 1,
                    background: 'rgba(108, 92, 231, 0.15)',
                    color: 'hsl(270, 100%, 75%)',
                    border: '1px solid hsla(270, 89%, 65%, 0.3)',
                    boxShadow: 'none',
                    padding: '0.6rem',
                    fontSize: '0.85rem',
                    margin: 0
                  }}
                >
                  🟢 Easy ({activeCard?.intervalDays * 2}d)
                </button>
                <button
                  className="btn-retry"
                  onClick={handleFlip}
                  style={{
                    flex: 0.8,
                    background: 'rgba(255, 255, 255, 0.03)',
                    color: 'var(--text-secondary)',
                    border: '1px solid var(--border-color)',
                    boxShadow: 'none',
                    padding: '0.6rem',
                    fontSize: '0.85rem',
                    margin: 0
                  }}
                >
                  👈 Question
                </button>
              </>
            )}
          </div>
        </div>
      )}
    </div>
  );
};
export default FlashcardsView;
