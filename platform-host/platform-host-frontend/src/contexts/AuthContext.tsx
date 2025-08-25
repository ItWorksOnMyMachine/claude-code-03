import React, { createContext, useContext, useEffect, useState, useCallback } from 'react';

interface User {
  id: string;
  email?: string;
  name?: string;
  claims?: Record<string, string>;
}

interface TenantInfo {
  id: string;
  name: string;
  userRoles: string[];
  isPlatformAdmin: boolean;
}

interface SessionResponse {
  isAuthenticated: boolean;
  user?: User;
  expiresAt?: string;
  selectedTenant?: TenantInfo;
}

interface AuthContextType {
  isAuthenticated: boolean;
  isLoading: boolean;
  user: User | null;
  selectedTenant: TenantInfo | null;
  login: (returnUrl?: string) => Promise<void>;
  logout: () => Promise<void>;
  checkSession: () => Promise<void>;
  clearTenant: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000';

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [user, setUser] = useState<User | null>(null);
  const [selectedTenant, setSelectedTenant] = useState<TenantInfo | null>(null);

  const checkSession = useCallback(async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/session`, {
        method: 'GET',
        credentials: 'include', // Include cookies
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        const data: SessionResponse = await response.json();
        setIsAuthenticated(data.isAuthenticated);
        setUser(data.user || null);
        setSelectedTenant(data.selectedTenant || null);
      } else {
        setIsAuthenticated(false);
        setUser(null);
        setSelectedTenant(null);
      }
    } catch (error) {
      console.error('Failed to check session:', error);
      setIsAuthenticated(false);
      setUser(null);
      setSelectedTenant(null);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const login = useCallback(async (returnUrl?: string) => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
        method: 'POST',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ returnUrl: returnUrl || window.location.pathname }),
      });

      if (response.ok) {
        const data = await response.json();
        // The API will return a redirect URL for OIDC flow
        if (data.redirectUrl) {
          window.location.href = data.redirectUrl;
        }
      } else {
        throw new Error('Login failed');
      }
    } catch (error) {
      console.error('Login error:', error);
      throw error;
    }
  }, []);

  const logout = useCallback(async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/logout`, {
        method: 'POST',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        setIsAuthenticated(false);
        setUser(null);
        setSelectedTenant(null);
        window.location.href = '/';
      } else {
        throw new Error('Logout failed');
      }
    } catch (error) {
      console.error('Logout error:', error);
      throw error;
    }
  }, []);

  const clearTenant = useCallback(async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/tenant/clear`, {
        method: 'POST',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        setSelectedTenant(null);
        // Optionally refresh session to get updated state
        await checkSession();
      } else {
        throw new Error('Failed to clear tenant selection');
      }
    } catch (error) {
      console.error('Clear tenant error:', error);
      throw error;
    }
  }, [checkSession]);

  // Check session on mount and periodically
  useEffect(() => {
    checkSession();

    // Check session every 30 seconds to detect external changes
    const interval = setInterval(checkSession, 30000);

    // Also check when window regains focus
    const handleFocus = () => checkSession();
    window.addEventListener('focus', handleFocus);

    return () => {
      clearInterval(interval);
      window.removeEventListener('focus', handleFocus);
    };
  }, [checkSession]);

  // Handle authentication callback
  useEffect(() => {
    const urlParams = new URLSearchParams(window.location.search);
    const authCallback = urlParams.get('auth_callback');
    const returnUrl = urlParams.get('returnUrl') || '/';
    
    if (authCallback === 'true') {
      // Clean up URL
      const newUrl = new URL(window.location.href);
      newUrl.searchParams.delete('auth_callback');
      newUrl.searchParams.delete('returnUrl');
      window.history.replaceState({}, document.title, newUrl.toString());
      
      // Check session after callback
      checkSession().then(() => {
        // After successful authentication, check if tenant needs to be selected
        // This will be handled by the protected route logic
      });
    }
  }, [checkSession]);

  const value: AuthContextType = {
    isAuthenticated,
    isLoading,
    user,
    selectedTenant,
    login,
    logout,
    checkSession,
    clearTenant,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};