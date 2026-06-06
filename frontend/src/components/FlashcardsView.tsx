import React, { useState, useEffect } from 'react';

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
  userId: number;
  onCardReviewed: () => void; // Call parent to track study stats
}

export const FlashcardsView: React.FC<FlashcardsViewProps> = ({
  topics,
  apiUrl,
  userId,
  onCardReviewed,
}) => {
  const [cards, setCards] = useState<Flashcard[]>([]);
  const [loading, setLoading] = useState(true);
  const [currentIdx, setCurrentIdx] = useState(0);
  const [isFlipped, setIsFlipped] = useState(false);
  const [reviewMode, setReviewMode] = useState<'due' | 'all'>('due');
  
  // Create Card Form State
  const [showAddForm, setShowAddForm] = useState(false);
  const [newFront, setNewFront] = useState('');
  const [newBack, setNewBack] = useState('');
  const [newTopicId, setNewTopicId] = useState(topics.length > 0 ? topics[0].id : 1);

  const fetchCards = async () => {
    setLoading(true);
    try {
      const res = await fetch(`${apiUrl}/api/flashcards`, {
        headers: {
          'X-User-Id': userId.toString()
        }
      });
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

  // Filter cards based on review mode (due vs all)
  const displayedCards = reviewMode === 'due'
    ? cards.filter(c => new Date(c.nextReviewDate) <= new Date())
    : cards;

  const handleFlip = () => {
    setIsFlipped(!isFlipped);
  };

  const handleReview = async (quality: 'easy' | 'hard') => {
    if (displayedCards.length === 0) return;
    const currentCard = displayedCards[currentIdx];

    try {
      const res = await fetch(`${apiUrl}/api/flashcards/${currentCard.id}/review?quality=${quality}`, {
        method: 'POST',
        headers: {
          'X-User-Id': userId.toString()
        }
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
      const res = await fetch(`${apiUrl}/api/flashcards`, {
        method: 'POST',
        headers: { 
          'Content-Type': 'application/json',
          'X-User-Id': userId.toString()
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
    <div className="study-area" style={{ height: '100%', overflowY: 'auto' }}>
      {/* Review Mode Selector */}
      <div style={{ display: 'flex', justifyContent: 'space-between', width: '100%', maxWidth: '520px', alignItems: 'center', marginBottom: '1rem' }}>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button
            className={`btn-status-toggle ${reviewMode === 'due' ? 'confident' : 'unseen'}`}
            onClick={() => { setReviewMode('due'); setCurrentIdx(0); setIsFlipped(false); }}
            style={{ borderRadius: '20px' }}
          >
            Due Today ({cards.filter(c => new Date(c.nextReviewDate) <= new Date()).length})
          </button>
          <button
            className={`btn-status-toggle ${reviewMode === 'all' ? 'confident' : 'unseen'}`}
            onClick={() => { setReviewMode('all'); setCurrentIdx(0); setIsFlipped(false); }}
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
            <button className="btn-retry" onClick={() => { setReviewMode('all'); setCurrentIdx(0); }}>
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
                <span className="card-side-label">Front</span>
                <p className="flashcard-text">{activeCard?.front}</p>
                <span style={{ position: 'absolute', bottom: '1rem', fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                  Click to Flip
                </span>
              </div>
              {/* Back side */}
              <div className="flashcard-back">
                <span className="card-side-label">Back / Answer</span>
                <p className="flashcard-text" style={{ whiteSpace: 'pre-line' }}>{activeCard?.back}</p>
                <span style={{ position: 'absolute', bottom: '1rem', fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                  Click to Flip Back
                </span>
              </div>
            </div>
          </div>

          {/* Review Buttons */}
          <div style={{ display: 'flex', gap: '1rem', width: '100%', maxWidth: '320px', marginTop: '1rem' }}>
            <button
              className="btn-retry"
              onClick={() => handleReview('hard')}
              style={{
                flex: 1,
                background: 'rgba(239, 68, 68, 0.15)',
                color: 'rgb(248, 113, 113)',
                border: '1px solid rgba(239, 68, 68, 0.3)',
                boxShadow: 'none',
              }}
            >
              Hard (1d)
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
              }}
            >
              Easy ({activeCard?.intervalDays * 2}d)
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
export default FlashcardsView;
