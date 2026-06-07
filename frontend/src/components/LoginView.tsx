import React, { useState } from 'react';

interface UserInfo {
  id: number;
  username: string;
  accessToken: string;
}

interface LoginViewProps {
  apiUrl: string;
  onLoginSuccess: (user: UserInfo) => void;
}

export default function LoginView({ apiUrl, onLoginSuccess }: LoginViewProps) {
  const [isRegister, setIsRegister] = useState(false);
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSuccessMsg(null);

    if (!username.trim() || !password.trim()) {
      setError('Please fill in all fields.');
      return;
    }

    if (isRegister && password !== confirmPassword) {
      setError('Passwords do not match.');
      return;
    }

    setLoading(true);

    try {
      const endpoint = isRegister ? '/api/auth/register' : '/api/auth/login';
      const res = await fetch(`${apiUrl}${endpoint}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        credentials: 'include',
        body: JSON.stringify({ username, password }),
      });

      const responseText = await res.text();
      let data: { message?: string; id?: number; username?: string; accessToken?: string } = {};
      try {
        data = responseText ? JSON.parse(responseText) : {};
      } catch {
        data = { message: responseText || 'Authentication failed' };
      }

      if (!res.ok) {
        throw new Error(data.message || 'Authentication failed');
      }

      if (isRegister) {
        setSuccessMsg('Registration successful! You can now log in.');
        setIsRegister(false);
        setPassword('');
        setConfirmPassword('');
      } else {
        if (data.id == null || !data.username || !data.accessToken) {
          throw new Error('Login response was missing user details.');
        }
        onLoginSuccess({ id: data.id, username: data.username, accessToken: data.accessToken });
      }
    } catch (err: any) {
      setError(err.message || 'An unexpected error occurred.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-container">
      <div className="login-glass-card">
        <div className="login-header">
          <div className="sidebar-logo login-logo">💡</div>
          <h1 className="login-title">TopicPortal Hub</h1>
          <p className="login-subtitle">
            {isRegister ? 'Create a secure account to save your notes' : 'Sign in to access your custom workspace'}
          </p>
        </div>

        {error && (
          <div className="login-alert error-alert">
            <span className="alert-icon">⚠️</span>
            <p>{error}</p>
          </div>
        )}

        {successMsg && (
          <div className="login-alert success-alert">
            <span className="alert-icon">✅</span>
            <p>{successMsg}</p>
          </div>
        )}

        <form onSubmit={handleSubmit} className="login-form">
          <div className="form-group">
            <label htmlFor="username">Username</label>
            <input
              type="text"
              id="username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="Enter your username"
              autoComplete="username"
              disabled={loading}
            />
          </div>

          <div className="form-group">
            <label htmlFor="password">Password</label>
            <input
              type="password"
              id="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Enter your password"
              autoComplete="current-password"
              disabled={loading}
            />
          </div>

          {isRegister && (
            <div className="form-group">
              <label htmlFor="confirmPassword">Confirm Password</label>
              <input
                type="password"
                id="confirmPassword"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                placeholder="Confirm your password"
                autoComplete="new-password"
                disabled={loading}
              />
            </div>
          )}

          <button type="submit" className="btn-login" disabled={loading}>
            {loading ? (
              <span className="spinner-small"></span>
            ) : isRegister ? (
              'Register Account'
            ) : (
              'Sign In'
            )}
          </button>
        </form>

        <div className="login-footer">
          <button
            type="button"
            className="toggle-mode-btn"
            onClick={() => {
              setIsRegister(!isRegister);
              setError(null);
              setSuccessMsg(null);
              setPassword('');
              setConfirmPassword('');
            }}
            disabled={loading}
          >
            {isRegister
              ? 'Already have an account? Sign In'
              : "Don't have an account? Register here"}
          </button>
        </div>
      </div>
    </div>
  );
}
