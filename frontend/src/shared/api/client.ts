import createClient, { type Middleware } from 'openapi-fetch';
import {
  accessTokenFor,
  expireSession,
  refreshSession,
  takeLoggedOutElsewhere,
} from '@/shared/auth/session';
import type { components, paths } from './schema';

const clientOptions = {
  baseUrl: window.location.origin,
  // resolve fetch per call, not at module load, so instrumentation (e.g. test stubs) is honoured
  fetch: (request: Request) => globalThis.fetch(request),
} as const;

/**
 * The authentication endpoints, without the session middleware: they carry the refresh cookie
 * themselves and must not trigger a refresh of their own.
 */
export const authClient = createClient<paths>(clientOptions);

/** The only way the frontend talks to the backend (docs/architecture.md, Communication rules). */
export const apiClient = createClient<paths>(clientOptions);

export type SystemStatus = components['schemas']['SystemStatus'];
export type AuthSession = components['schemas']['AuthSession'];
export type CurrentUser = components['schemas']['CurrentUser'];
export type DemoUser = components['schemas']['DemoUserView'];

export const REQUEST_TIMEOUT_MS = 10_000;

const retryCopies = new Map<string, Request>();

/**
 * Adds the in-memory access token (refreshing it first when it is about to expire) and, on a 401,
 * refreshes once and repeats the request; when the refresh fails the session ends (spec 0002, R-6 / R-8).
 */
export const sessionMiddleware: Middleware = {
  async onRequest({ request, id }) {
    if (takeLoggedOutElsewhere()) {
      expireSession(); // spec 0002, R-7: another tab logged out — this tab learns it on its next request
      throw new DOMException('Session ended', 'AbortError');
    }
    const token = await accessTokenFor();
    if (token) request.headers.set('Authorization', `Bearer ${token}`);
    retryCopies.set(id, request.clone());
    return request;
  },
  async onResponse({ response, id }) {
    const copy = retryCopies.get(id);
    retryCopies.delete(id);
    if (response.status !== 401 || !copy) return response;

    const session = await refreshSession();
    if (!session) {
      expireSession();
      return response;
    }
    copy.headers.set('Authorization', `Bearer ${session.accessToken}`);
    return globalThis.fetch(copy);
  },
  onError({ id }) {
    retryCopies.delete(id);
  },
};

apiClient.use(sessionMiddleware);

/** Aborts when the caller aborts or after the timeout (spec 0001, AC-4). */
export function withTimeout(signal: AbortSignal | undefined, timeoutMs = REQUEST_TIMEOUT_MS) {
  const controller = new AbortController();
  const timer = setTimeout(
    () => controller.abort(new DOMException('Timeout', 'TimeoutError')),
    timeoutMs,
  );
  const onAbort = () => controller.abort(signal?.reason);
  signal?.addEventListener('abort', onAbort, { once: true });
  // Settle on abort even if the underlying fetch ignores the signal.
  const aborted = new Promise<never>((_, reject) =>
    controller.signal.addEventListener('abort', () => reject(controller.signal.reason), {
      once: true,
    }),
  );
  aborted.catch(() => {}); // observed via race(); avoid an unhandled rejection when unused
  return {
    signal: controller.signal,
    race: <T>(request: Promise<T>) => Promise.race([request, aborted]),
    dispose: () => {
      clearTimeout(timer);
      signal?.removeEventListener('abort', onAbort);
    },
  };
}
