import { act, screen, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it, vi } from 'vitest';
import { REQUEST_TIMEOUT_MS } from '@/shared/api/client';
import { renderApp } from '@/test/renderApp';
import { healthyStatus, server, statusResponse, statusUrl } from '@/test/server';

function card() {
  return screen.getByRole('region', { name: 'Sistem Durumu' });
}

describe('SystemStatusCard (spec 0001 AC-3 – AC-6)', () => {
  it('shows loading while waiting, then API up, database reachable and the department count', async () => {
    let release!: () => void;
    const gate = new Promise<void>((resolve) => (release = resolve));
    server.use(
      http.get(statusUrl, async () => {
        await gate;
        return HttpResponse.json(healthyStatus);
      }),
    );
    renderApp('/');

    expect(
      within(card()).getByRole('status', { name: 'Sistem durumu yükleniyor' }),
    ).toBeInTheDocument();
    release();

    expect(await within(card()).findByText('Çalışıyor')).toBeInTheDocument();
    expect(within(card()).getByText('Erişilebilir')).toBeInTheDocument();
    expect(within(card()).getByText('6')).toBeInTheDocument();
    expect(within(card()).queryByRole('status')).not.toBeInTheDocument();
  });

  it('when the database is unreachable shows "Erişilemiyor" and "—" for the count', async () => {
    server.use(statusResponse({ api: 'up', database: 'unavailable', departmentCount: null }));
    renderApp('/');

    const unavailable = await within(card()).findByText('Erişilemiyor');
    expect(unavailable.closest('[data-tone]')).toHaveAttribute('data-tone', 'danger');
    expect(within(card()).getByText('Çalışıyor')).toBeInTheDocument();
    expect(within(card()).getByText('—')).toBeInTheDocument();
  });

  it('when the database has no departments shows "Tanımlı departman yok"', async () => {
    server.use(statusResponse({ api: 'up', database: 'available', departmentCount: 0 }));
    renderApp('/');

    expect(await within(card()).findByText('Tanımlı departman yok')).toBeInTheDocument();
  });

  it('when the API cannot be reached shows a Turkish error with "Tekrar dene", which recovers', async () => {
    server.use(http.get(statusUrl, () => HttpResponse.error()));
    const { user } = renderApp('/');

    const alert = await within(card()).findByRole('alert');
    expect(alert).toHaveTextContent('Sistem durumu alınamadı. Sunucuya şu anda ulaşılamıyor.');

    server.use(statusResponse(healthyStatus));
    await user.click(within(alert).getByRole('button', { name: 'Tekrar dene' }));

    expect(await within(card()).findByText('Erişilebilir')).toBeInTheDocument();
    expect(within(card()).queryByRole('alert')).not.toBeInTheDocument();
  });

  it('treats a server error response as unreachable', async () => {
    server.use(http.get(statusUrl, () => new HttpResponse(null, { status: 500 })));
    renderApp('/');

    expect(await within(card()).findByRole('alert')).toBeInTheDocument();
  });

  it('shows the error state exactly when the API has not answered for 10 seconds', async () => {
    // Only the timer functions are faked: promises, MSW and React keep running on real microtasks.
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] });
    try {
      let requested!: () => void;
      const inFlight = new Promise<void>((resolve) => (requested = resolve));
      server.use(
        http.get(statusUrl, () => {
          requested();
          return new Promise<never>(() => {});
        }),
      );
      renderApp('/');
      await act(() => inFlight);

      await act(() => vi.advanceTimersByTimeAsync(REQUEST_TIMEOUT_MS - 1));
      expect(
        within(card()).getByRole('status', { name: 'Sistem durumu yükleniyor' }),
      ).toBeInTheDocument();
      expect(within(card()).queryByRole('alert')).not.toBeInTheDocument();

      // At 10 000 ms the request is aborted. react-query publishes the result on a zero-delay
      // timer, which fake timers schedule 1 ms later when it is created inside a timer callback.
      await act(() => vi.advanceTimersByTimeAsync(1));
      await act(() => vi.advanceTimersByTimeAsync(1));
      expect(within(card()).getByRole('alert')).toHaveTextContent('Sistem durumu alınamadı');
    } finally {
      vi.useRealTimers();
    }
  });

  it('while the card shows an error, the menu and the theme keep working', async () => {
    server.use(http.get(statusUrl, () => HttpResponse.error()));
    const { user, router } = renderApp('/');
    await within(card()).findByRole('alert');

    await user.click(screen.getByRole('radio', { name: 'Koyu' }));
    expect(document.documentElement).toHaveClass('dark');

    await user.click(screen.getByRole('link', { name: 'Ayarlar' }));
    expect(router.state.location.pathname).toBe('/ayarlar');
  });
});
