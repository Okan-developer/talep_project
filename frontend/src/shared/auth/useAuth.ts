import { createContext, use } from 'react';
import type { CurrentUser } from '@/shared/api/client';

export type LoginOutcome =
  | { ok: true }
  | { ok: false; kind: 'invalid' | 'throttled' | 'network' }
  | { ok: false; kind: 'locked'; message?: string };

export interface AuthContextValue {
  status: 'restoring' | 'authenticated' | 'anonymous';
  user: CurrentUser | null;
  /** The session ended without a logout: the login screen tells the user (spec 0002, R-8). */
  sessionExpired: boolean;
  /** Whether the user holds a permission — for showing or hiding UI only; the server decides. */
  can: (permission: string) => boolean;
  login: (email: string, password: string) => Promise<LoginOutcome>;
  logout: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

export function useAuth(): AuthContextValue {
  const value = use(AuthContext);
  if (!value) throw new Error('useAuth must be used inside AuthProvider');
  return value;
}
