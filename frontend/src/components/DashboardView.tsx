import React, { useState, useEffect } from 'react';
import CalendarHeatmap from 'react-calendar-heatmap';
import 'react-calendar-heatmap/dist/styles.css';

interface StudySession {
  id: number;
  durationSeconds: number;
  notesReviewedCount: number;
  cardsReviewedCount: number;
  questionsAttemptedCount: number;
  createdAt: string;
}

interface Topic {
  id: number;
  title: string;
}

interface HeatmapValue {
  date: string;
  count: number;
  duration: number;
}

interface StatsData {
  heatmap: HeatmapValue[];
  confidentQuestions: { topicId: number; count: number }[];
  dueFlashcards: { topicId: number; count: number }[];
}

interface DashboardViewProps {
  topics: Topic[];
  apiUrl: string;
  sessionTrigger: number; // Used to trigger refresh when parent logs a session
}

export const DashboardView: React.FC<DashboardViewProps> = ({
  topics,
  apiUrl,
  sessionTrigger,
}) => {
  const [sessions, setSessions] = useState<StudySession[]>([]);
  const [stats, setStats] = useState<StatsData>({ heatmap: [], confidentQuestions: [], dueFlashcards: [] });
  const [loading, setLoading] = useState(true);

  const fetchDashboardData = async () => {
    setLoading(true);
    try {
      // Fetch study logs
      const sessionsRes = await fetch(`${apiUrl}/api/studysessions`);
      if (!sessionsRes.ok) throw new Error('Failed to load study sessions');
      const sessionsData = await sessionsRes.json();
      setSessions(sessionsData);

      // Fetch aggregated analytics (heatmap, due cards, confident questions)
      const statsRes = await fetch(`${apiUrl}/api/studysessions/stats`);
      if (!statsRes.ok) throw new Error('Failed to load stats');
      const statsData = await statsRes.json();
      setStats(statsData);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDashboardData();
  }, [sessionTrigger]);

  const formatDuration = (seconds: number) => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    if (mins === 0) return `${secs}s`;
    return `${mins}m ${secs}s`;
  };

  const getHeatmapRange = () => {
    const today = new Date();
    // Show 120 days of activity
    const startDate = new Date();
    startDate.setDate(today.getDate() - 120);
    return { startDate, endDate: today };
  };

  const { startDate, endDate } = getHeatmapRange();

  // Aggregate totals
  const totalStudySeconds = sessions.reduce((acc, s) => acc + s.durationSeconds, 0);
  const totalCardsReviewed = sessions.reduce((acc, s) => acc + s.cardsReviewedCount, 0);
  const totalQuestionsAttempted = sessions.reduce((acc, s) => acc + s.questionsAttemptedCount, 0);

  // Find due cards / confident counts for a specific topic
  const getTopicStats = (topicId: number) => {
    const confident = stats.confidentQuestions.find(cq => cq.topicId === topicId)?.count || 0;
    const due = stats.dueFlashcards.find(df => df.topicId === topicId)?.count || 0;
    return { confident, due };
  };

  if (loading) {
    return (
      <div className="loader-container">
        <div className="spinner"></div>
        <p style={{ color: 'var(--text-secondary)' }}>Calculating analytics metrics...</p>
      </div>
    );
  }

  return (
    <div className="dashboard-grid">
      <h2 style={{ fontSize: '1.5rem', fontWeight: '600', marginBottom: '2rem', background: 'var(--accent-gradient)', WebkitBackgroundClip: 'text', WebkitTextFillColor: 'transparent', backgroundClip: 'text' }}>
        Progress & Analytics Dashboard
      </h2>

      {/* Stats Cards Row */}
      <div className="stats-grid">
        <div className="stat-card">
          <span className="stat-label">Total Study Time</span>
          <span className="stat-value">{formatDuration(totalStudySeconds)}</span>
        </div>
        <div className="stat-card">
          <span className="stat-label">Cards Reviewed</span>
          <span className="stat-value">{totalCardsReviewed}</span>
        </div>
        <div className="stat-card">
          <span className="stat-label">Quizzes Answered</span>
          <span className="stat-value">{totalQuestionsAttempted}</span>
        </div>
        <div className="stat-card">
          <span className="stat-label">Completed Sessions</span>
          <span className="stat-value">{sessions.length}</span>
        </div>
      </div>

      {/* GitHub Heatmap Panel */}
      <div className="dashboard-panel-box" style={{ marginBottom: '2.5rem' }}>
        <h3 className="dashboard-panel-title">Study Activity Calendar</h3>
        <div style={{ marginTop: '1rem', width: '100%', overflowX: 'auto' }}>
          <div style={{ minWidth: '700px' }}>
            <CalendarHeatmap
              startDate={startDate}
              endDate={endDate}
              values={stats.heatmap}
              classForValue={(value: any) => {
                if (!value || value.count === 0) return 'color-empty';
                if (value.count === 1) return 'color-scale-1';
                if (value.count === 2) return 'color-scale-2';
                if (value.count === 3) return 'color-scale-3';
                return 'color-scale-4'; // 4+ sessions
              }}
              titleForValue={(value: any) => {
                if (!value) return 'No sessions';
                return `${value.count} session(s) on ${value.date} (${Math.round(value.duration / 60)} mins)`;
              }}
              showWeekdayLabels
            />
          </div>
        </div>
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', fontSize: '0.7rem', color: 'var(--text-muted)', marginTop: '0.75rem' }}>
          <span>Less</span>
          <span style={{ width: '10px', height: '10px', background: 'hsla(245, 22%, 10%, 0.4)', borderRadius: '2px' }}></span>
          <span style={{ width: '10px', height: '10px', background: 'hsla(270, 89%, 65%, 0.2)', borderRadius: '2px' }}></span>
          <span style={{ width: '10px', height: '10px', background: 'hsla(270, 89%, 65%, 0.45)', borderRadius: '2px' }}></span>
          <span style={{ width: '10px', height: '10px', background: 'hsla(270, 89%, 65%, 0.7)', borderRadius: '2px' }}></span>
          <span style={{ width: '10px', height: '10px', background: 'var(--accent-cyan)', borderRadius: '2px' }}></span>
          <span>More</span>
        </div>
      </div>

      <div className="dashboard-row">
        {/* Topic Breakdown Progress */}
        <div className="dashboard-panel-box">
          <h3 className="dashboard-panel-title">Per-Topic Metrics</h3>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
            {topics.map(topic => {
              const { confident, due } = getTopicStats(topic.id);
              return (
                <div key={topic.id} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', paddingBottom: '0.75rem', borderBottom: '1px solid hsla(245, 100%, 80%, 0.03)' }}>
                  <div>
                    <h4 style={{ fontSize: '0.92rem', fontWeight: '500' }}>🏷️ {topic.title}</h4>
                  </div>
                  <div style={{ display: 'flex', gap: '0.75rem' }}>
                    <span className="tag-difficulty confident" style={{ fontSize: '0.7rem', display: 'flex', alignItems: 'center', gap: '0.2rem' }}>
                      🟢 {confident} Confident
                    </span>
                    <span className="tag-difficulty medium" style={{ fontSize: '0.7rem', display: 'flex', alignItems: 'center', gap: '0.2rem' }}>
                      🎴 {due} Due Today
                    </span>
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        {/* Study Log list */}
        <div className="dashboard-panel-box">
          <h3 className="dashboard-panel-title">Recent Sessions</h3>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', maxHeight: '350px', overflowY: 'auto' }}>
            {sessions.length === 0 ? (
              <span style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>No study sessions logged yet.</span>
            ) : (
              sessions.slice(0, 10).map((session, idx) => (
                <div key={session.id || idx} style={{ background: 'rgba(255, 255, 255, 0.01)', border: '1px solid var(--border-color)', borderRadius: '8px', padding: '0.75rem 1rem' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.75rem', color: 'var(--text-muted)', marginBottom: '0.35rem' }}>
                    <span>Session #{sessions.length - idx}</span>
                    <span>{new Date(session.createdAt).toLocaleDateString()}</span>
                  </div>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <span style={{ fontSize: '0.9rem', color: 'var(--accent-cyan)' }}>⏳ {formatDuration(session.durationSeconds)}</span>
                    <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>
                      🎴{session.cardsReviewedCount} • 📝{session.notesReviewedCount} • 🎯{session.questionsAttemptedCount}
                    </span>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
export default DashboardView;
