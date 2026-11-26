import { screen, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
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
});
