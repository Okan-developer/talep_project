import { http, HttpResponse } from 'msw';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { getSession, hasExpiredNotice, setSession } from '@/shared/auth/session';
import {
  adminUser,
  healthyStatus,
  refreshFails,
  refreshReturns,
  refreshUrl,
  server,
  sessionFor,
  statusUrl,
} from '@/test/server';
import { apiClient, authClient, REQUEST_TIMEOUT_MS, withTimeout } from './client';

describe('withTimeout (spec 0001 AC-4: 10 seconds)', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('the request timeout is 10 seconds', () => {
    expect(REQUEST_TIMEOUT_MS).toBe(10_000);
  });

  it('aborts exactly when the timeout elapses and settles a request that never answers', async () => {
    vi.useFakeTimers();
    const timeout = withTimeout(undefined);
    const outcome = timeout.race(new Promise<never>(() => {})).then(
      () => 'resolved',
      (error: unknown) => (error as DOMException).name,
    );

    vi.advanceTimersByTime(REQUEST_TIMEOUT_MS - 1);
    expect(timeout.signal.aborted).toBe(false);

    vi.advanceTimersByTime(1);
    expect(timeout.signal.aborted).toBe(true);
    await expect(outcome).resolves.toBe('TimeoutError');
  });

  it('follows the caller abort and stops the timer on dispose', () => {
    vi.useFakeTimers();
    const caller = new AbortController();
    const timeout = withTimeout(caller.signal);

    caller.abort();
    expect(timeout.signal.aborted).toBe(true);

    const other = withTimeout(undefined);
    other.dispose();
    vi.advanceTimersByTime(REQUEST_TIMEOUT_MS);
    expect(other.signal.aborted).toBe(false);
  });
});

describe('apiClient session middleware (spec 0002, R-6 / R-8 / AC-9)', () => {
  it('sends the in-memory access token as a bearer token', async () => {
    const session = setSession(sessionFor(adminUser));
    let authorization: string | null = null;
    server.use(
      http.get(statusUrl, ({ request }) => {
        authorization = request.headers.get('Authorization');
        return HttpResponse.json(healthyStatus);
      }),
    );

    await apiClient.GET('/api/system/status');

    expect(authorization).toBe(`Bearer ${session.accessToken}`);
  });

  it('on a 401 refreshes once and repeats the request with the new token', async () => {
    setSession(sessionFor(adminUser));
    const seen: (string | null)[] = [];
    server.use(
      refreshReturns(adminUser),
      http.get(statusUrl, ({ request }) => {
        seen.push(request.headers.get('Authorization'));
        return seen.length === 1
          ? new HttpResponse(null, { status: 401 })
          : HttpResponse.json(healthyStatus);
      }),
    );

    const { data, response } = await apiClient.GET('/api/system/status');

    expect(response.status).toBe(200);
    expect(data).toEqual(healthyStatus);
    expect(seen).toHaveLength(2);
    expect(seen[1]).not.toBe(seen[0]);
    expect(seen[1]).toBe(`Bearer ${getSession()?.accessToken}`);
  });

  it('on a 401 with a refused refresh, ends the session with the expired notice', async () => {
    setSession(sessionFor(adminUser));
    server.use(
      refreshFails(),
      http.get(statusUrl, () => new HttpResponse(null, { status: 401 })),
    );

    const { response } = await apiClient.GET('/api/system/status');

    expect(response.status).toBe(401);
    expect(getSession()).toBeNull();
    expect(hasExpiredNotice()).toBe(true);
  });

  it('when the access token is due and the refresh is refused, refreshes once and sends nothing', async () => {
    setSession(sessionFor(adminUser, Date.now() - 15 * 60_000)); // access token already expired
    let refreshes = 0;
    let requests = 0;
    server.use(
      http.post(refreshUrl, () => {
        refreshes += 1;
        return new HttpResponse(null, { status: 401 });
      }),
      http.get(statusUrl, () => {
        requests += 1;
        return HttpResponse.json(healthyStatus);
      }),
    );

    await expect(apiClient.GET('/api/system/status')).rejects.toThrow();

    expect(refreshes).toBe(1);
    expect(requests).toBe(0);
    expect(getSession()).toBeNull();
    expect(hasExpiredNotice()).toBe(true);
  });

  it('without a session a 401 is returned as is, with no refresh attempt', async () => {
    let refreshes = 0;
    server.use(
      http.post(refreshUrl, () => {
        refreshes += 1;
        return new HttpResponse(null, { status: 401 });
      }),
      http.get(statusUrl, () => new HttpResponse(null, { status: 401 })),
    );

    const { response } = await apiClient.GET('/api/system/status');

    expect(response.status).toBe(401);
    expect(refreshes).toBe(0);
  });

  it('after another tab logs out, the next request ends this tab’s session', async () => {
    setSession(sessionFor(adminUser));
    const otherTab = new BroadcastChannel('talep-auth');
    otherTab.postMessage('logout');
    otherTab.close();
    await new Promise((resolve) => setTimeout(resolve, 20));

    await expect(apiClient.GET('/api/system/status')).rejects.toThrow();

    expect(getSession()).toBeNull();
    expect(hasExpiredNotice()).toBe(true);
  });

  it('the auth endpoints go without the middleware (no bearer token, no refresh loop)', async () => {
    setSession(sessionFor(adminUser));
    let authorization: string | null = 'unset';
    let refreshes = 0;
    server.use(
      http.post(refreshUrl, ({ request }) => {
        refreshes += 1;
        authorization = request.headers.get('Authorization');
        return new HttpResponse(null, { status: 401 });
      }),
    );

    await authClient.POST('/api/auth/refresh');

    expect(authorization).toBeNull();
    expect(refreshes).toBe(1);
  });
});
