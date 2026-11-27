import { act, fireEvent, screen, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it, vi } from 'vitest';
import { REQUEST_TIMEOUT_MS } from '@/shared/api/client';
import { getSession } from '@/shared/auth/session';
import { renderApp } from '@/test/renderApp';
import { logoutUrl, server, userWith } from '@/test/server';

describe('top bar (spec 0002, R-7)', () => {
  it('AC-2: shows the signed-in user’s name and "Çıkış"', () => {
    renderApp('/', { user: userWith(['Requester'], 'Ayşe Yılmaz') });

    const header = screen.getByRole('banner');
    expect(within(header).getByText('Ayşe Yılmaz')).toBeInTheDocument();
    expect(within(header).getByText('Giriş yapan kullanıcı:', { exact: false })).toHaveClass(
      'sr-only',
    );
    expect(within(header).getByRole('button', { name: 'Çıkış' })).toBeInTheDocument();
  });

  it('AC-9: "Çıkış" ends the session on the server and returns to the login screen without the expiry notice', async () => {
    let logouts = 0;
    server.use(
      http.post(logoutUrl, () => {
        logouts += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const { user, router } = renderApp('/raporlar');

    await user.click(screen.getByRole('button', { name: 'Çıkış' }));

    expect(await screen.findByRole('heading', { level: 1, name: 'Giriş yap' })).toBeInTheDocument();
    expect(router.state.location.pathname).toBe('/giris');
    expect(router.state.location.search).toBe('');
    expect(logouts).toBe(1);
    expect(getSession()).toBeNull();
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });

  it('AC-9: logout still ends the local session when the server cannot be reached', async () => {
    server.use(http.post(logoutUrl, () => HttpResponse.error()));
    const { user } = renderApp('/');

    await user.click(screen.getByRole('button', { name: 'Çıkış' }));

    expect(await screen.findByRole('heading', { level: 1, name: 'Giriş yap' })).toBeInTheDocument();
    expect(getSession()).toBeNull();
  });

  it('AC-9: logout returns to the login screen after 10 seconds when the server never answers', async () => {
    // Only the timer functions are faked: promises, MSW and React keep running on real microtasks.
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] });
    try {
      let requested!: () => void;
      const inFlight = new Promise<void>((resolve) => (requested = resolve));
      server.use(
        http.post(logoutUrl, () => {
          requested();
          return new Promise<never>(() => {});
        }),
      );
      const { router } = renderApp('/raporlar');

      fireEvent.click(screen.getByRole('button', { name: 'Çıkış' }));
      await act(() => inFlight);
      await act(() => vi.advanceTimersByTimeAsync(REQUEST_TIMEOUT_MS - 1));
      expect(router.state.location.pathname).toBe('/raporlar');

      await act(() => vi.advanceTimersByTimeAsync(1));
      expect(router.state.location.pathname).toBe('/giris');
      expect(screen.getByRole('heading', { level: 1, name: 'Giriş yap' })).toBeInTheDocument();
      expect(getSession()).toBeNull();
    } finally {
      vi.useRealTimers();
    }
  });
});
