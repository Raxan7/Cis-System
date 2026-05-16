/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { loadSession, sessionChangeEventName, type StoredSession } from '../lib/auth-storage';
import { login as apiLogin, logout as apiLogout, setSession } from '../lib/http';

type AuthContextValue = {
  session: StoredSession | null;
  login: (email: string, password: string, mfaCode?: string | null) => Promise<void>;
  logout: () => Promise<void>;
  updateSession: (session: StoredSession | null) => void;
};

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSessionState] = useState<StoredSession | null>(() => loadSession());

  useEffect(() => {
    const syncSession = () => setSessionState(loadSession());
    window.addEventListener(sessionChangeEventName, syncSession);
    return () => window.removeEventListener(sessionChangeEventName, syncSession);
  }, []);

  const value = useMemo<AuthContextValue>(() => ({
    session,
    login: async (email, password, mfaCode) => {
      const nextSession = await apiLogin(email, password, mfaCode);
      setSessionState(nextSession);
    },
    logout: async () => {
      await apiLogout();
      setSessionState(null);
    },
    updateSession: (nextSession) => {
      setSession(nextSession);
      setSessionState(nextSession);
    },
  }), [session]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }

  return context;
}
