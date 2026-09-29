import { createContext } from 'react';
import type { AuthResult } from '../api/types';

export interface AuthContextValue {
  session: AuthResult | null;
  signIn: (session: AuthResult) => void;
  signOut: () => void;
}

export const AuthContext = createContext<AuthContextValue | null>(null);
