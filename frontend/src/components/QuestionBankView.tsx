import React, { useState, useEffect } from 'react';
import apiFetch from '../api';

interface Question {
  id: number;
  text: string;
  answer: string;
  topicId: number;
  difficulty: string; // "Easy", "Medium", "Hard"
  status: string; // "Unseen", "Attempted", "Confident"
}

interface Topic {
  id: number;
  title: string;
  description?: string;
  createdAt?: string;
}

interface QuestionBankViewProps {
  topics: Topic[];
  apiUrl: string;
  onQuestionAttempted: () => void; // Call parent to track study session stats
}

export const QuestionBankView: React.FC<QuestionBankViewProps> = ({
  topics,
  apiUrl,
  onQuestionAttempted,
}) => {
  const [allQuestions, setAllQuestions] = useState<Question[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTopicFilter, setActiveTopicFilter] = useState<number | null>(null);
  const [activeDiffFilter, setActiveDiffFilter] = useState<string | null>(null);
  
  // Expand accordion track
  const [expandedQuestionId, setExpandedQuestionId] = useState<number | null>(null);

  // Create Form State
  const [showAddForm, setShowAddForm] = useState(false);
  const [newText, setNewText] = useState('');
  const [newAnswer, setNewAnswer] = useState('');
  const [newTopicId, setNewTopicId] = useState(topics.length > 0 ? topics[0].id : 1);
  const [newDifficulty, setNewDifficulty] = useState('Medium');

  // Synchronize newTopicId when topics load asynchronously
  useEffect(() => {
    if (topics.length > 0) {
      setNewTopicId(topics[0].id);
    }
  }, [topics]);

  const fetchQuestions = async () => {
    setLoading(true);
    try {
      const res = await apiFetch(`${apiUrl}/api/questions`);
      if (!res.ok) throw new Error('Failed to load questions');
      const data = await res.json();
      setAllQuestions(data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchQuestions();
  }, []);

  // Filter questions locally
  const displayedQuestions = allQuestions.filter(q => {
    const matchesTopic = !activeTopicFilter || q.topicId === activeTopicFilter;
    const matchesDiff = !activeDiffFilter || q.difficulty.toLowerCase() === activeDiffFilter.toLowerCase();
    return matchesTopic && matchesDiff;
  });

  // Calculate stats for the selected topic
  const selectedTopicQuestions = allQuestions.filter(q => !activeTopicFilter || q.topicId === activeTopicFilter);
  const totalCount = selectedTopicQuestions.length;
  const confidentCount = selectedTopicQuestions.filter(q => q.status === 'Confident').length;
  const attemptedCount = selectedTopicQuestions.filter(q => q.status === 'Attempted').length;
  const unseenCount = selectedTopicQuestions.filter(q => q.status === 'Unseen').length;
  const progressPercent = totalCount > 0 ? Math.round((confidentCount / totalCount) * 100) : 0;

  const selectedTopic = activeTopicFilter ? topics.find(t => t.id === activeTopicFilter) : null;

  // Toggle status cycle: Unseen -> Attempted -> Confident -> Unseen
  const handleStatusCycle = async (id: number, currentStatus: string) => {
    let nextStatus = 'Unseen';
    if (currentStatus === 'Unseen') nextStatus = 'Attempted';
    else if (currentStatus === 'Attempted') nextStatus = 'Confident';

    try {
      const res = await apiFetch(`${apiUrl}/api/questions/${id}/status`, {
        method: 'PUT',
        headers: { 
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(nextStatus),
      });

      if (!res.ok) throw new Error('Failed to update status');
      const updatedQuestion = await res.json();

      setAllQuestions(prev => prev.map(q => q.id === updatedQuestion.id ? updatedQuestion : q));
      
      // Increment attempt counter if moving from Unseen to Attempted
      if (currentStatus === 'Unseen') {
        onQuestionAttempted();
      }
    } catch (err) {
      console.error(err);
    }
  };

  const handleCreateQuestion = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newText || !newAnswer) return;

    const payload = {
      text: newText,
      answer: newAnswer,
      topicId: newTopicId,
      difficulty: newDifficulty,
    };

    try {
      const res = await apiFetch(`${apiUrl}/api/questions`, {
        method: 'POST',
        headers: { 
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(payload),
      });

      if (!res.ok) throw new Error('Failed to create question');
      const createdQuestion = await res.json();

      setAllQuestions(prev => [createdQuestion, ...prev]);
      setNewText('');
      setNewAnswer('');
      setShowAddForm(false);
      alert('Question added successfully!');
    } catch (err: any) {
      alert(`Error: ${err.message}`);
    }
  };

  const toggleAccordion = (id: number) => {
    setExpandedQuestionId(expandedQuestionId === id ? null : id);
  };

  return (
    <div className="study-area" style={{ height: '100%', overflowY: 'auto', justifyContent: 'flex-start' }}>
      {/* Filtering Header bar */}
      <div className="dashboard-panel-box" style={{ width: '100%', marginBottom: '1.5rem', padding: '1.25rem' }}>
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: '1rem', alignItems: 'center' }}>
          
          {/* Topic Select */}
          <div style={{ flex: '1 1 200px' }}>
            <label style={{ display: 'block', fontSize: '0.75rem', color: 'var(--text-secondary)', marginBottom: '0.25rem' }}>Topic</label>
            <select
              className="notes-search-input"
              value={activeTopicFilter || ''}
              onChange={e => {
                setActiveTopicFilter(e.target.value ? Number(e.target.value) : null);
                setExpandedQuestionId(null);
              }}
              style={{ background: 'var(--bg-darker)', color: 'var(--text-primary)' }}
            >
              <option value="">All Topics</option>
              {topics.map(t => (
                <option key={t.id} value={t.id}>{t.title}</option>
              ))}
            </select>
          </div>

          {/* Difficulty Select */}
          <div style={{ flex: '1 1 150px' }}>
            <label style={{ display: 'block', fontSize: '0.75rem', color: 'var(--text-secondary)', marginBottom: '0.25rem' }}>Difficulty</label>
            <select
              className="notes-search-input"
              value={activeDiffFilter || ''}
              onChange={e => {
                setActiveDiffFilter(e.target.value || null);
                setExpandedQuestionId(null);
              }}
              style={{ background: 'var(--bg-darker)', color: 'var(--text-primary)' }}
            >
              <option value="">All Difficulties</option>
              <option value="Easy">Easy</option>
              <option value="Medium">Medium</option>
              <option value="Hard">Hard</option>
            </select>
          </div>

          {/* Add Question Button */}
          <button
            className="btn-new-note"
            onClick={() => setShowAddForm(!showAddForm)}
            style={{ marginTop: '1.25rem', height: '36px' }}
          >
            {showAddForm ? 'Cancel' : '+ Add Question'}
          </button>
        </div>
      </div>

      {/* Add Question Form Box */}
      {showAddForm && (
        <form onSubmit={handleCreateQuestion} className="dashboard-panel-box" style={{ width: '100%', marginBottom: '1.5rem' }}>
          <h3 className="dashboard-panel-title">Add Practice Question</h3>
          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', fontSize: '0.8rem', color: 'var(--text-secondary)', marginBottom: '0.35rem' }}>Question Description</label>
            <textarea
              className="notes-search-input"
              rows={2}
              style={{ width: '100%', resize: 'none', height: 'auto', padding: '0.5rem' }}
              placeholder="e.g. Explain state management difference between Props and State in React."
              value={newText}
              onChange={e => setNewText(e.target.value)}
              required
            />
          </div>
          <div style={{ marginBottom: '1.25rem' }}>
            <label style={{ display: 'block', fontSize: '0.8rem', color: 'var(--text-secondary)', marginBottom: '0.35rem' }}>Answer / Correct Explanation</label>
            <textarea
              className="notes-search-input"
              rows={3}
              style={{ width: '100%', resize: 'none', height: 'auto', padding: '0.5rem' }}
              placeholder="Props are immutable data passed down from parents; State is local mutable data managed within the component."
              value={newAnswer}
              onChange={e => setNewAnswer(e.target.value)}
              required
            />
          </div>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '1rem', alignItems: 'center' }}>
            <div style={{ flex: '1 1 180px' }}>
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
            <div style={{ flex: '1 1 120px' }}>
              <label style={{ display: 'block', fontSize: '0.8rem', color: 'var(--text-secondary)', marginBottom: '0.35rem' }}>Difficulty</label>
              <select
                className="notes-search-input"
                value={newDifficulty}
                onChange={e => setNewDifficulty(e.target.value)}
                style={{ background: 'var(--bg-darker)', color: 'var(--text-primary)' }}
              >
                <option value="Easy">Easy</option>
                <option value="Medium">Medium</option>
                <option value="Hard">Hard</option>
              </select>
            </div>
            <button type="submit" className="btn-new-note" style={{ marginTop: '1.25rem', height: '36px' }}>Save Question</button>
          </div>
        </form>
      )}

      {/* Topic Overview & Progress Details Card */}
      <div className="dashboard-panel-box" style={{ width: '100%', marginBottom: '2rem', padding: '1.5rem', background: 'radial-gradient(circle at top right, rgba(6, 182, 212, 0.04), transparent)' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: '1rem', marginBottom: '0.75rem' }}>
          <div>
            <h3 style={{ fontSize: '1.2rem', fontWeight: '600', color: 'var(--text-primary)', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              🏷️ {selectedTopic ? selectedTopic.title : 'All Practice Topics Overview'}
            </h3>
            <p style={{ fontSize: '0.88rem', color: 'var(--text-secondary)', marginTop: '0.35rem', fontWeight: '350', maxWidth: '650px', lineHeight: '1.5' }}>
              {selectedTopic ? selectedTopic.description : 'Test your readiness and review practice questions across all categories to prepare for your tech interviews.'}
            </p>
          </div>
          <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end' }}>
            <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Confidence Score</span>
            <span style={{ fontSize: '1.75rem', fontWeight: '700', color: 'var(--accent-cyan)', background: 'var(--accent-gradient)', WebkitBackgroundClip: 'text', WebkitTextFillColor: 'transparent', backgroundClip: 'text' }}>
              {progressPercent}%
            </span>
          </div>
        </div>

        {/* Progress Bar */}
        <div style={{ width: '100%', height: '8px', background: 'var(--bg-darker)', borderRadius: '4px', overflow: 'hidden', marginBottom: '1.25rem', border: '1px solid var(--border-color)' }}>
          <div style={{ width: `${progressPercent}%`, height: '100%', background: 'var(--accent-gradient)', borderRadius: '4px', transition: 'width 0.5s ease' }}></div>
        </div>

        {/* Topic Breakdown Badges */}
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.75rem', fontSize: '0.75rem' }}>
          <span className="tag-difficulty confident" style={{ display: 'flex', alignItems: 'center', gap: '0.25rem', padding: '0.25rem 0.6rem', fontSize: '0.7rem' }}>
            🟢 {confidentCount} Confident
          </span>
          <span className="tag-difficulty medium" style={{ display: 'flex', alignItems: 'center', gap: '0.25rem', padding: '0.25rem 0.6rem', fontSize: '0.7rem' }}>
            ⚠️ {attemptedCount} Attempted
          </span>
          <span className="tag-difficulty unseen" style={{ display: 'flex', alignItems: 'center', gap: '0.25rem', padding: '0.25rem 0.6rem', fontSize: '0.7rem', background: 'rgba(255, 255, 255, 0.03)', color: 'var(--text-muted)', border: '1px solid var(--border-color)' }}>
            🔘 {unseenCount} Unseen
          </span>
          <span className="tag-difficulty easy" style={{ display: 'flex', alignItems: 'center', gap: '0.25rem', padding: '0.25rem 0.6rem', fontSize: '0.7rem', background: 'rgba(6, 182, 212, 0.08)', border: '1px solid hsla(190, 90%, 50%, 0.15)', color: 'var(--accent-cyan)' }}>
            📝 {totalCount} Total Questions
          </span>
        </div>
      </div>

      {/* Quizzes List */}
      {loading ? (
        <div className="loader-container">
          <div className="spinner"></div>
          <p style={{ color: 'var(--text-secondary)' }}>Loading quizzes...</p>
        </div>
      ) : displayedQuestions.length === 0 ? (
        <div className="error-container" style={{ width: '100%', borderColor: 'var(--border-color)', margin: '4rem 0' }}>
          <h2 className="error-title" style={{ color: 'var(--accent-cyan)' }}>No Questions Available</h2>
          <p className="error-msg">No practice questions match the selected filters. Change filters or add a new question.</p>
        </div>
      ) : (
        <div style={{ width: '100%' }}>
          {displayedQuestions.map((q) => (
            <div key={q.id} className="question-item-box">
              <div className="question-header">
                <div className="question-tags">
                  <span className={`tag-difficulty ${q.difficulty.toLowerCase()}`}>
                    {q.difficulty}
                  </span>
                  <span style={{ fontSize: '0.7rem', color: 'var(--text-muted)', background: 'rgba(255, 255, 255, 0.03)', padding: '0.15rem 0.45rem', borderRadius: '4px', border: '1px solid var(--border-color)' }}>
                    {topics.find(t => t.id === q.topicId)?.title || 'General'}
                  </span>
                </div>
                {/* Tri-State toggle unseen -> attempted -> confident */}
                <button
                  className={`btn-status-toggle ${q.status.toLowerCase()}`}
                  onClick={() => handleStatusCycle(q.id, q.status)}
                >
                  {q.status === 'Unseen' && '🔘 Unseen'}
                  {q.status === 'Attempted' && '⚠️ Attempted'}
                  {q.status === 'Confident' && '✅ Confident'}
                </button>
              </div>

              <h4 style={{ fontSize: '1.05rem', fontWeight: '400', marginBottom: '1rem', lineHeight: '1.5' }}>
                {q.text}
              </h4>

              {/* Reveal Explanation Accordion */}
              <button
                className="btn-retry"
                onClick={() => toggleAccordion(q.id)}
                style={{
                  padding: '0.4rem 0.8rem',
                  fontSize: '0.75rem',
                  background: 'var(--bg-darker)',
                  border: '1px solid var(--border-color)',
                  color: 'var(--text-secondary)',
                  boxShadow: 'none',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '0.25rem',
                }}
              >
                {expandedQuestionId === q.id ? 'Hide Explanation ▲' : 'Show Explanation ▼'}
              </button>

              {expandedQuestionId === q.id && (
                <div style={{ marginTop: '1rem', padding: '1rem', background: 'rgba(255, 255, 255, 0.02)', borderLeft: '3px solid var(--accent-cyan)', borderRadius: '0 8px 8px 0', fontSize: '0.92rem', color: 'var(--text-secondary)', lineHeight: '1.6', animation: 'pulse 0.3s ease' }}>
                  <strong>Explanation:</strong>
                  <p style={{ marginTop: '0.35rem', fontWeight: '300' }}>{q.answer}</p>
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
export default QuestionBankView;
