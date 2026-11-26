import { useEffect, useState, useSyncExternalStore, type ReactNode } from 'react';
import { authClient, withTimeout } from '@/shared/api/client';
import { AuthContext, type AuthContextValue, type LoginOutcome } from './useAuth';
import {
  clearSession,
  getSession,
  hasExpiredNotice,
  refreshSession,
  setSession,
  subscribe,
} from './session';

interface ProblemBody {
  code?: string;
  detail?: string;
}

/**
 * Restores the session once on application start (refresh cookie → access token) and exposes the
 * signed-in user, login and logout (spec 0002, R-3 / R-6 / R-7 / R-8).
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const session = useSyncExternalStore(subscribe, getSession);
  // A session already in memory (e.g. set by a test) needs no restore.
  const [restoring, setRestoring] = useState(() => getSession() === null);

  useEffect(() => {
    if (!restoring) return;
    let active = true;
    void refreshSession().finally(() => {
      if (active) setRestoring(false);
    });
    return () => {
      active = false;
    };
  }, [restoring]);

  const value: AuthContextValue = {
    status: restoring ? 'restoring' : session ? 'authenticated' : 'anonymous',
    user: session?.user ?? null,
    sessionExpired: !session && hasExpiredNotice(),
    can: (permission) => session?.user.permissions.includes(permission) ?? false,
    login,
    logout,
  };
  return <AuthContext value={value}>{children}</AuthContext>;
}

async function login(email: string, password: string): Promise<LoginOutcome> {
  const timeout = withTimeout(undefined);
  try {
    const { data, error, response } = await timeout.race(
      authClient.POST('/api/auth/login', {
        body: { email, password },
        signal: timeout.signal,
        credentials: 'same-origin',
      }),
    );
    if (response.ok && data) {
      setSession(data);
      return { ok: true };
    }
    const problem = (error ?? {}) as ProblemBody;
    if (response.status === 429) return { ok: false, kind: 'throttled' };
    if (problem.code === 'account_locked')
      return { ok: false, kind: 'locked', message: problem.detail };
    if (response.status === 400) return { ok: false, kind: 'invalid' };
    if (response.status === 401) return { ok: false, kind: 'invalid' };
    return { ok: false, kind: 'network' };
  } catch {
    return { ok: false, kind: 'network' };
  } finally {
    timeout.dispose();
  }
}

async function logout() {
  try {
    await authClient.POST('/api/auth/logout', { credentials: 'same-origin' });
  } catch {
    // The server session ends with the cookie's lifetime at worst; this device forgets it now.
  } finally {
    clearSession();
  }
}
