import { http, HttpResponse } from 'msw';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { adminUser, refreshFails, refreshUrl, server, sessionFor } from '@/test/server';
import {
  accessTokenFor,
  expireSession,
  getSession,
  hasExpiredNotice,
  REFRESH_LOCK,
  REFRESH_MARGIN_MS,
  refreshSession,
  setSession,
  clearSession,
} from './session';

describe('session (spec 0002, R-6 / AC-7)', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('keeps the access token in memory only — no browser storage, no readable cookie', () => {
    setSession(sessionFor(adminUser));

    expect(getSession()?.accessToken).toMatch(/^token-/);
    expect(window.localStorage.length).toBe(0);
    expect(window.sessionStorage.length).toBe(0);
    expect(document.cookie).toBe('');
  });

  it('uses the current token while it is valid for longer than the margin', async () => {
    const session = setSession(sessionFor(adminUser));
    let refreshes = 0;
    server.use(
      http.post(refreshUrl, () => {
        refreshes += 1;
        return HttpResponse.json(sessionFor(adminUser));
      }),
    );

    const token = await accessTokenFor(session.expiresAt - REFRESH_MARGIN_MS - 1);

    expect(token).toBe(session.accessToken);
    expect(refreshes).toBe(0);
  });

  it('past 15 minutes, refreshes silently and returns the new token', async () => {
    const first = setSession(sessionFor(adminUser));
    server.use(http.post(refreshUrl, () => HttpResponse.json(sessionFor(adminUser))));

    const token = await accessTokenFor(first.expiresAt + 1);

    expect(token).not.toBe(first.accessToken);
    expect(getSession()?.accessToken).toBe(token);
  });

  it('concurrent refreshes in one tab share a single request', async () => {
    let refreshes = 0;
    server.use(
      http.post(refreshUrl, async () => {
        refreshes += 1;
        await new Promise((resolve) => setTimeout(resolve, 10));
        return HttpResponse.json(sessionFor(adminUser));
      }),
    );

    const [a, b] = await Promise.all([refreshSession(), refreshSession()]);

    expect(refreshes).toBe(1);
    expect(a).toBe(b);
  });

  it('serialises refreshes across tabs with a Web Lock', async () => {
    const request = vi.fn((_name: string, work: () => Promise<unknown>) => work());
    vi.stubGlobal('navigator', { ...navigator, locks: { request } });
    server.use(http.post(refreshUrl, () => HttpResponse.json(sessionFor(adminUser))));

    await refreshSession();

    expect(request).toHaveBeenCalledWith(REFRESH_LOCK, expect.any(Function));
  });

  it('a refused refresh resolves null and ends the session with the expired notice', async () => {
    setSession(sessionFor(adminUser));
    server.use(refreshFails());

    const token = await accessTokenFor(Number.MAX_SAFE_INTEGER);

    expect(token).toBeNull();
    expect(getSession()).toBeNull();
    expect(hasExpiredNotice()).toBe(true);
  });

  it('logout clears the session without the expired notice', () => {
    setSession(sessionFor(adminUser));

    clearSession();

    expect(getSession()).toBeNull();
    expect(hasExpiredNotice()).toBe(false);
  });

  it('expiring when nobody is signed in changes nothing', () => {
    expireSession();

    expect(hasExpiredNotice()).toBe(false);
  });
});
