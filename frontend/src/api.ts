let accessToken: string | null = null;
let onLogoutCallback: (() => void) | null = null;

export const setAccessToken = (token: string | null) => {
  accessToken = token;
};

export const setOnLogoutCallback = (callback: () => void) => {
  onLogoutCallback = callback;
};

interface ApiFetchOptions extends RequestInit {
  headers?: Record<string, string>;
}

export const apiFetch = async (url: string, options: ApiFetchOptions = {}): Promise<Response> => {
  // 1. Prepare headers
  const headers: Record<string, string> = { ...options.headers };
  if (accessToken) {
    headers['Authorization'] = `Bearer ${accessToken}`;
  }

  // Ensure credentials: 'include' is set for all API calls to pass the HttpOnly cookie
  const fetchOptions: RequestInit = {
    ...options,
    headers,
    credentials: 'include',
  };

  // 2. Make the initial request
  let response = await fetch(url, fetchOptions);

  // 3. Handle 401 Unauthorized (Expired Access Token)
  if (response.status === 401) {
    // Prevent infinite loop if the refresh endpoint itself returns 401
    if (url.endsWith('/api/auth/refresh')) {
      if (onLogoutCallback) onLogoutCallback();
      return response;
    }

    // Attempt to refresh the token
    try {
      const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5100';
      const refreshRes = await fetch(`${apiUrl}/api/auth/refresh`, {
        method: 'POST',
        credentials: 'include',
      });

      if (refreshRes.ok) {
        const data = await refreshRes.json();
        const newAccessToken = data.accessToken;
        setAccessToken(newAccessToken);

        // Retry the original request with the new access token
        const retryHeaders: Record<string, string> = { ...options.headers };
        if (newAccessToken) {
          retryHeaders['Authorization'] = `Bearer ${newAccessToken}`;
        }
        return await fetch(url, {
          ...options,
          headers: retryHeaders,
          credentials: 'include',
        });
      } else {
        // Refresh token is expired or invalid
        if (onLogoutCallback) onLogoutCallback();
      }
    } catch (err) {
      console.error('Error refreshing token:', err);
      if (onLogoutCallback) onLogoutCallback();
    }
  }

  return response;
};
export default apiFetch;
