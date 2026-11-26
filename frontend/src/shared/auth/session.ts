import { authClient, withTimeout, type AuthSession } from '@/shared/api/client';

/**
 * The browser side of the session (spec 0002, R-6 / R-7; docs/security.md, AuthN). The access token
 * lives only in this module's memory — never in localStorage, sessionStorage or a readable cookie.
 * The refresh token is an httpOnly cookie the browser sends to /api/auth by itself.
 */
export interface Session {
  accessToken: string;
  expiresAt: number;
  user: AuthSession['user'];
}

/** Refresh this long before the access token expires, so a request never leaves with a stale one. */
export const REFRESH_MARGIN_MS = 30_000;
export const REFRESH_LOCK = 'talep-auth-refresh';
const CHANNEL = 'talep-auth';

let current: Session | null = null;
let expiredNotice = false;
let loggedOutElsewhere = false;
let inflight: Promise<Session | null> | null = null;
const listeners = new Set<() => void>();

const channel = typeof BroadcastChannel === 'undefined' ? null : new BroadcastChannel(CHANNEL);
channel?.addEventListener('message', (event: MessageEvent) => {
  if (event.data === 'logout' && current) loggedOutElsewhere = true;
});

function notify() {
  for (const listener of listeners) listener();
}

export function getSession() {
  return current;
}

export function subscribe(listener: () => void) {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function setSession(session: AuthSession) {
  current = {
    accessToken: session.accessToken,
    expiresAt: Date.parse(session.expiresAt),
    user: session.user,
  };
  expiredNotice = false;
  loggedOutElsewhere = false;
  notify();
  return current;
}

/** Ends the session because the server no longer accepts it; the login screen says so (R-8). */
export function expireSession() {
  if (!current) return;
  current = null;
  expiredNotice = true;
  notify();
}

/** Ends the session on purpose (logout): no "session expired" notice; other tabs are told. */
export function clearSession() {
  current = null;
  expiredNotice = false;
  channel?.postMessage('logout');
  notify();
}

/** Whether the login screen should say the session expired. Reading it does not clear it. */
export function hasExpiredNotice() {
  return expiredNotice;
}

/** True once after another tab logged out of this session (R-7). */
export function takeLoggedOutElsewhere() {
  const value = loggedOutElsewhere;
  loggedOutElsewhere = false;
  return value;
}

/** The access token to send now; refreshed first when it expires within the margin. */
export async function accessTokenFor(now = Date.now()) {
  if (!current) return null;
  if (current.expiresAt - now > REFRESH_MARGIN_MS) return current.accessToken;
  const session = await refreshSession();
  if (!session) expireSession();
  return session?.accessToken ?? null;
}

/**
 * Rotates the refresh cookie and stores the new access token. One refresh at a time per tab
 * (concurrent callers share it) and across tabs (Web Locks), so two tabs never present the same
 * rotated cookie — which the server would treat as reuse and end the session (R-6, AC-7).
 * Resolves null when the server refuses the session (or cannot be reached).
 */
export function refreshSession(): Promise<Session | null> {
  inflight ??= withCrossTabLock(doRefresh).finally(() => {
    inflight = null;
  });
  return inflight;
}

async function doRefresh(): Promise<Session | null> {
  const timeout = withTimeout(undefined);
  try {
    const { data, response } = await timeout.race(
      authClient.POST('/api/auth/refresh', { signal: timeout.signal, credentials: 'same-origin' }),
    );
    return response.ok && data ? setSession(data) : null;
  } catch {
    return null;
  } finally {
    timeout.dispose();
  }
}

function withCrossTabLock<T>(work: () => Promise<T>): Promise<T> {
  const locks = typeof navigator === 'undefined' ? undefined : navigator.locks;
  return locks?.request ? (locks.request(REFRESH_LOCK, work) as Promise<T>) : work();
}

/** Test seam: forget everything between tests. */
export function resetSessionForTests() {
  current = null;
  expiredNotice = false;
  loggedOutElsewhere = false;
  inflight = null;
  listeners.clear();
}
