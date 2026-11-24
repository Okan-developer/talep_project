import createClient from 'openapi-fetch';
import type { components, paths } from './schema';

/** The only way the frontend talks to the backend (docs/architecture.md, Communication rules). */
export const apiClient = createClient<paths>({
  baseUrl: window.location.origin,
  // resolve fetch per call, not at module load, so instrumentation (e.g. test stubs) is honoured
  fetch: (request) => globalThis.fetch(request),
});

export type SystemStatus = components['schemas']['SystemStatus'];

export const REQUEST_TIMEOUT_MS = 10_000;

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
