import { act, screen } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { navItems } from '@/app/navigation';
import { expireSession } from '@/shared/auth/session';
import { allMenuUser, renderApp } from '@/test/renderApp';
import {
  healthyStatus,
  loginUrl,
  refreshFails,
  refreshUrl,
  server,
  sessionFor,
  statusUrl,
  userWith,
} from '@/test/server';

describe('routing (spec 0001 AC-2)', () => {
  it.each(navItems.filter((item) => item.path !== '/').map((item) => [item.label]))(
    'menu item "%s" opens its "Bu ekran hazırlanıyor" page and is highlighted',
    async (label) => {
      const { user } = renderApp('/');

      await user.click(screen.getByRole('link', { name: label }));

      expect(screen.getByRole('heading', { level: 1, name: label })).toBeInTheDocument();
      expect(screen.getByText('Bu ekran hazırlanıyor')).toBeInTheDocument();
      expect(screen.getByRole('link', { name: label })).toHaveAttribute('aria-current', 'page');
      expect(screen.getByRole('link', { name: 'Genel Bakış' })).not.toHaveAttribute('aria-current');
    },
  );

  it('an unknown address shows "Sayfa bulunamadı" with a way back to Genel Bakış', async () => {
    const { user, router } = renderApp('/boyle-bir-sayfa-yok');

    expect(screen.getByRole('heading', { level: 1, name: 'Sayfa bulunamadı' })).toBeInTheDocument();
    await user.click(screen.getByRole('link', { name: 'Genel Bakış’a dön' }));

    expect(router.state.location.pathname).toBe('/');
    expect(screen.getByRole('heading', { level: 1, name: 'Genel Bakış' })).toBeInTheDocument();
  });
});

async function signIn(user: ReturnType<typeof renderApp>['user']) {
  await user.type(screen.getByLabelText('E-posta'), 'admin@demo.local');
  await user.type(screen.getByLabelText('Parola'), 'Demo.2026!');
  await user.click(screen.getByRole('button', { name: 'Giriş yap' }));
}

describe('sign-in routing (spec 0002, R-8 / R-11)', () => {
  it('AC-10: a signed-out visitor is sent to the login screen and returns to the address after login', async () => {
    server.use(http.post(loginUrl, () => HttpResponse.json(sessionFor(allMenuUser))));
    const { user, router } = renderApp('/raporlar', { user: null });

    expect(await screen.findByRole('heading', { level: 1, name: 'Giriş yap' })).toBeInTheDocument();
    expect(router.state.location.pathname).toBe('/giris');
    expect(router.state.location.search).toBe('?returnTo=%2Fraporlar');

    await signIn(user);

    expect(await screen.findByRole('heading', { level: 1, name: 'Raporlar' })).toBeInTheDocument();
    expect(router.state.location.pathname).toBe('/raporlar');
  });

  it('AC-10: while the session is restored a full-page loader shows, then the page — no login flash', async () => {
    let release!: () => void;
    const gate = new Promise<void>((resolve) => (release = resolve));
    server.use(
      http.post(refreshUrl, async () => {
        await gate;
        return HttpResponse.json(sessionFor(allMenuUser));
      }),
    );
    renderApp('/raporlar', { user: null });

    expect(screen.getByRole('status', { name: 'Oturum yükleniyor' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Giriş yap' })).not.toBeInTheDocument();
    release();

    expect(await screen.findByRole('heading', { level: 1, name: 'Raporlar' })).toBeInTheDocument();
  });

  it('AC-10: when the session cannot be refreshed the user lands on login with the notice, then returns after login', async () => {
    server.use(
      http.get(statusUrl, () => new HttpResponse(null, { status: 401 })),
      refreshFails(),
    );
    const { user, router } = renderApp('/');

    expect(
      await screen.findByText('Oturumunuz sona erdi. Lütfen tekrar giriş yapın.'),
    ).toHaveAttribute('role', 'status');
    expect(router.state.location.pathname).toBe('/giris');

    server.use(
      http.get(statusUrl, () => HttpResponse.json(healthyStatus)),
      http.post(loginUrl, () => HttpResponse.json(sessionFor(allMenuUser))),
    );
    await signIn(user);

    expect(
      await screen.findByRole('heading', { level: 1, name: 'Genel Bakış' }),
    ).toBeInTheDocument();
    expect(router.state.location.pathname).toBe('/');
  });

  it('AC-10: a session that ends on a deeper page brings the user back to that page after login', async () => {
    server.use(http.post(loginUrl, () => HttpResponse.json(sessionFor(allMenuUser))));
    const { user, router } = renderApp('/raporlar');
    expect(screen.getByRole('heading', { level: 1, name: 'Raporlar' })).toBeInTheDocument();

    act(() => expireSession());

    expect(await screen.findByRole('status')).toHaveTextContent('Oturumunuz sona erdi.');
    expect(router.state.location.search).toBe('?returnTo=%2Fraporlar');
    await signIn(user);
    expect(await screen.findByRole('heading', { level: 1, name: 'Raporlar' })).toBeInTheDocument();
  });

  it('a signed-in user opening the login address goes on to Genel Bakış', async () => {
    const { router } = renderApp('/giris');

    expect(
      await screen.findByRole('heading', { level: 1, name: 'Genel Bakış' }),
    ).toBeInTheDocument();
    expect(router.state.location.pathname).toBe('/');
  });

  it('AC-15: an address missing from the user’s menu shows "yetki yok" with a way back; the menu keeps working', async () => {
    const { user, router } = renderApp('/yonetim/kullanicilar', {
      user: userWith(['SoftwareStaff'], 'BT Yazılım Personeli'),
    });

    expect(
      screen.getByRole('heading', { level: 1, name: 'Bu sayfayı görüntüleme yetkiniz yok' }),
    ).toBeInTheDocument();
    expect(screen.queryByText('Bu ekran hazırlanıyor')).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Kullanıcı Yönetimi' })).not.toBeInTheDocument();

    await user.click(screen.getByRole('link', { name: 'Bana Atanan İşler' }));
    expect(
      screen.getByRole('heading', { level: 1, name: 'Bana Atanan İşler' }),
    ).toBeInTheDocument();

    await router.navigate('/ayarlar');
    expect(
      await screen.findByRole('heading', { level: 1, name: 'Bu sayfayı görüntüleme yetkiniz yok' }),
    ).toBeInTheDocument();
    await user.click(screen.getByRole('link', { name: 'Genel Bakış’a dön' }));
    expect(router.state.location.pathname).toBe('/');
  });
});
