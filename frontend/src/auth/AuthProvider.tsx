import { useCallback, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import type { AuthResult } from '../api/types';
import { AuthContext } from './context';
import { clearSession, loadSession, saveSession } from './session';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AuthResult | null>(() => loadSession());

  const signIn = useCallback((next: AuthResult) => {
    saveSession(next);
    setSession(next);
  }, []);

  const signOut = useCallback(() => {
    clearSession();
    setSession(null);
  }, []);

  const value = useMemo(() => ({ session, signIn, signOut }), [session, signIn, signOut]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
