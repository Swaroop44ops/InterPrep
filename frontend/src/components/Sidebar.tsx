import React from 'react';

interface Topic {
  id: number;
  title: string;
  description: string;
}

interface Note {
  id: number;
  title: string;
  topicId: number;
}

interface SidebarProps {
  topics: Topic[];
  activeTopicId: number | null;
  onSelectTopic: (id: number | null) => void;
  notes: Note[];
}

export const Sidebar: React.FC<SidebarProps> = ({
  topics,
  activeTopicId,
  onSelectTopic,
  notes,
}) => {
  // Compute counts locally from current notes list
  const getNoteCount = (topicId: number | null) => {
    if (topicId === null) {
      return notes.length;
    }
    return notes.filter((n) => n.topicId === topicId).length;
  };

  return (
    <aside className="sidebar-panel">
      <div className="sidebar-header">
        <div className="sidebar-logo">T</div>
        <span className="sidebar-title">Topic Explorer</span>
      </div>
      <div className="sidebar-scrollable">
        <h2 className="sidebar-section-title">Workspace</h2>
        <ul className="sidebar-menu-list">
          <li
            className={`sidebar-menu-item ${activeTopicId === null ? 'active' : ''}`}
            onClick={() => onSelectTopic(null)}
          >
            <span>📁 All Notes</span>
            <span className="sidebar-count-badge">{getNoteCount(null)}</span>
          </li>
        </ul>

        <h2 className="sidebar-section-title" style={{ marginTop: '2rem' }}>
          Filters By Topic
        </h2>
        <ul className="sidebar-menu-list">
          {topics.map((topic) => (
            <li
              key={topic.id}
              className={`sidebar-menu-item ${activeTopicId === topic.id ? 'active' : ''}`}
              onClick={() => onSelectTopic(topic.id)}
              title={topic.description}
            >
              <span style={{ textOverflow: 'ellipsis', overflow: 'hidden', whiteSpace: 'nowrap' }}>
                🏷️ {topic.title}
              </span>
              <span className="sidebar-count-badge">{getNoteCount(topic.id)}</span>
            </li>
          ))}
        </ul>
      </div>
    </aside>
  );
};
export default Sidebar;
