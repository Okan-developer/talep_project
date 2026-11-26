import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { delay, http, HttpResponse } from 'msw';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { createMemoryRouter, RouterProvider, useLocation } from 'react-router';
import { AuthProvider } from '@/shared/auth/AuthProvider';
import { expireSession, getSession, setSession } from '@/shared/auth/session';
import {
  adminUser,
  demoUsers,
  demoUsersUrl,
  loginUrl,
  server,
  sessionFor,
  userWith,
} from '@/test/server';
import { LoginPage } from './LoginPage';

function Landing() {
  const location = useLocation();
  return <p>landed:{location.pathname}</p>;
}

function renderLogin(path = '/giris', userEventOptions?: Parameters<typeof userEvent.setup>[0]) {
  const router = createMemoryRouter(
    [
      { path: '/giris', element: <LoginPage /> },
      { path: '*', element: <Landing /> },
    ],
    { initialEntries: [path] },
  );
  const user = userEvent.setup(userEventOptions);
  render(
    <QueryClientProvider client={new QueryClient()}>
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
    </QueryClientProvider>,
  );
  return { user, router };
}

/** Records login request bodies and answers with `respond`. */
function loginHandler(respond: () => Response | Promise<Response>) {
  const bodies: { email: string; password: string }[] = [];
  server.use(
    http.post(loginUrl, async ({ request }) => {
      bodies.push((await request.json()) as { email: string; password: string });
      return respond();
    }),
  );
  return bodies;
}

const problem = (status: number, body: Record<string, unknown>) =>
  HttpResponse.json(body, { status, headers: { 'Content-Type': 'application/problem+json' } });

async function fillAndSubmit(
  user: ReturnType<typeof userEvent.setup>,
  email: string,
  password: string,
) {
  await user.type(screen.getByLabelText('E-posta'), email);
  await user.type(screen.getByLabelText('Parola'), password);
  await user.click(screen.getByRole('button', { name: 'Giriş yap' }));
}

describe('LoginPage (spec 0002)', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('empty state: labelled e-mail and password fields and the "Giriş yap" button; no demo list outside Development', async () => {
    renderLogin();

    expect(screen.getByRole('heading', { level: 1, name: 'Giriş yap' })).toBeInTheDocument();
    expect(screen.getByLabelText('E-posta')).toHaveValue('');
    expect(screen.getByLabelText('Parola')).toHaveAttribute('type', 'password');
    expect(screen.getByRole('button', { name: 'Giriş yap' })).toBeEnabled();
    await waitFor(() => expect(screen.queryByText('Demo kullanıcı seç')).not.toBeInTheDocument());
  });

  it('AC-2: a successful login goes to the return address', async () => {
    loginHandler(() => HttpResponse.json(sessionFor(adminUser)));
    const { user } = renderLogin('/giris?returnTo=%2Fraporlar');

    await fillAndSubmit(user, 'admin@demo.local', 'Demo.2026!');

    expect(await screen.findByText('landed:/raporlar')).toBeInTheDocument();
    expect(getSession()?.user.fullName).toBe(adminUser.fullName);
  });

  it('AC-10: a return address outside the application leads to Genel Bakış', async () => {
    loginHandler(() => HttpResponse.json(sessionFor(adminUser)));
    const { user } = renderLogin('/giris?returnTo=https%3A%2F%2Fkotu.example');

    await fillAndSubmit(user, 'admin@demo.local', 'Demo.2026!');

    expect(await screen.findByText('landed:/')).toBeInTheDocument();
  });

  it('AC-3: empty fields show the Turkish required message under each field and send nothing', async () => {
    const bodies = loginHandler(() => HttpResponse.json(sessionFor(adminUser)));
    const { user } = renderLogin();

    await user.click(screen.getByRole('button', { name: 'Giriş yap' }));

    const email = screen.getByLabelText('E-posta');
    const password = screen.getByLabelText('Parola');
    expect(email).toHaveAttribute('aria-invalid', 'true');
    expect(email).toHaveAccessibleDescription('Bu alan zorunludur.');
    expect(password).toHaveAccessibleDescription('Bu alan zorunludur.');
    expect(screen.getAllByRole('alert').map((a) => a.textContent)).toEqual([
      'Bu alan zorunludur.',
      'Bu alan zorunludur.',
    ]);
    expect(email).toHaveFocus();
    expect(bodies).toHaveLength(0);
  });

  it('AC-3: a wrong password shows the shared message, keeps the e-mail and clears the password', async () => {
    loginHandler(() =>
      problem(401, { code: 'invalid_credentials', detail: 'E-posta ya da parola hatalı.' }),
    );
    const { user } = renderLogin();

    await fillAndSubmit(user, 'admin@demo.local', 'yanlis');

    expect(await screen.findByRole('alert')).toHaveTextContent('E-posta ya da parola hatalı.');
    expect(screen.getByLabelText('E-posta')).toHaveValue('admin@demo.local');
    expect(screen.getByLabelText('Parola')).toHaveValue('');
  });

  it('AC-4: a locked account shows the server message with the remaining minutes', async () => {
    loginHandler(() =>
      problem(401, {
        code: 'account_locked',
        detail: 'Hesap geçici olarak kilitlendi. 15 dakika sonra tekrar deneyin.',
      }),
    );
    const { user } = renderLogin();

    await fillAndSubmit(user, 'admin@demo.local', 'Demo.2026!');

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Hesap geçici olarak kilitlendi. 15 dakika sonra tekrar deneyin.',
    );
  });

  it('AC-5: the throttle answer shows "Çok fazla deneme yapıldı…"', async () => {
    loginHandler(() => problem(429, { title: 'Çok fazla istek' }));
    const { user } = renderLogin();

    await fillAndSubmit(user, 'admin@demo.local', 'Demo.2026!');

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Çok fazla deneme yapıldı. Lütfen biraz sonra tekrar deneyin.',
    );
  });

  it('AC-19: while submitting the button is disabled and a second click sends nothing', async () => {
    let release!: () => void;
    const gate = new Promise<void>((resolve) => (release = resolve));
    const bodies = loginHandler(async () => {
      await gate;
      return HttpResponse.json(sessionFor(adminUser));
    });
    const { user } = renderLogin();
    await user.type(screen.getByLabelText('E-posta'), 'admin@demo.local');
    await user.type(screen.getByLabelText('Parola'), 'Demo.2026!');

    await user.click(screen.getByRole('button', { name: 'Giriş yap' }));
    const busy = await screen.findByRole('button', { name: 'Giriş yapılıyor…' });
    expect(busy).toBeDisabled();
    await user.click(busy);
    release();

    expect(await screen.findByText('landed:/')).toBeInTheDocument();
    expect(bodies).toHaveLength(1);
  });

  it('AC-19: when the API cannot be reached the message appears and the form can be sent again', async () => {
    let fail = true;
    loginHandler(() => (fail ? HttpResponse.error() : HttpResponse.json(sessionFor(adminUser))));
    const { user } = renderLogin();

    await fillAndSubmit(user, 'admin@demo.local', 'Demo.2026!');
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Sunucuya ulaşılamadı. Lütfen tekrar deneyin.',
    );

    fail = false;
    await user.type(screen.getByLabelText('Parola'), 'Demo.2026!');
    await user.click(screen.getByRole('button', { name: 'Giriş yap' }));

    expect(await screen.findByText('landed:/')).toBeInTheDocument();
  });

  it('AC-19: no answer within 10 seconds shows the message and re-enables the form', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    loginHandler(async () => {
      await delay('infinite');
      return HttpResponse.json(sessionFor(adminUser));
    });
    const { user } = renderLogin('/giris', { advanceTimers: vi.advanceTimersByTime });
    await user.type(screen.getByLabelText('E-posta'), 'admin@demo.local');
    await user.type(screen.getByLabelText('Parola'), 'Demo.2026!');
    await user.click(screen.getByRole('button', { name: 'Giriş yap' }));
    expect(await screen.findByRole('button', { name: 'Giriş yapılıyor…' })).toBeDisabled();

    await vi.advanceTimersByTimeAsync(10_000);

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Sunucuya ulaşılamadı. Lütfen tekrar deneyin.',
    );
    expect(screen.getByRole('button', { name: 'Giriş yap' })).toBeEnabled();
  });

  it('AC-6: in Development the seven demo users are listed; picking one logs in through the normal login', async () => {
    server.use(http.get(demoUsersUrl, () => HttpResponse.json(demoUsers)));
    const bodies = loginHandler(() =>
      HttpResponse.json(sessionFor(userWith(['SoftwareStaff'], 'BT Yazılım Personeli'))),
    );
    const { user } = renderLogin();

    const list = await screen.findByRole('region', { name: 'Demo kullanıcı seç' });
    const buttons = within(list).getAllByRole('button');
    expect(buttons).toHaveLength(7);
    expect(buttons.map((b) => b.textContent)).toEqual(
      demoUsers.map((u) => expect.stringContaining(u.fullName)),
    );
    await user.click(within(list).getByRole('button', { name: /BT Yazılım Personeli/ }));

    expect(await screen.findByText('landed:/')).toBeInTheDocument();
    expect(bodies).toEqual([{ email: 'bt.yazilim@demo.local', password: 'Demo.2026!' }]);
  });

  it('AC-17: the form is filled and sent with the keyboard only, Enter submits', async () => {
    const bodies = loginHandler(() => HttpResponse.json(sessionFor(adminUser)));
    const { user } = renderLogin();

    await user.tab();
    expect(screen.getByLabelText('E-posta')).toHaveFocus();
    await user.keyboard('admin@demo.local');
    await user.tab();
    expect(screen.getByLabelText('Parola')).toHaveFocus();
    await user.keyboard('Demo.2026!{Enter}');

    expect(await screen.findByText('landed:/')).toBeInTheDocument();
    expect(bodies).toHaveLength(1);
  });

  it('AC-17: the demo list is reachable with the keyboard', async () => {
    server.use(http.get(demoUsersUrl, () => HttpResponse.json(demoUsers)));
    const bodies = loginHandler(() => HttpResponse.json(sessionFor(adminUser)));
    const { user } = renderLogin();
    await screen.findByRole('region', { name: 'Demo kullanıcı seç' });

    await user.tab(); // e-mail
    await user.tab(); // password
    await user.tab(); // submit
    await user.tab(); // first demo user
    expect(screen.getByRole('button', { name: /Talep Sahibi/ })).toHaveFocus();
    await user.keyboard('{Enter}');

    await waitFor(() => expect(bodies).toHaveLength(1));
    expect(bodies[0]?.email).toBe('talep.sahibi@demo.local');
  });

  it('AC-10: after the session expired the screen says so', async () => {
    setSession(sessionFor(adminUser));
    expireSession();

    renderLogin();

    expect(screen.getByRole('status')).toHaveTextContent(
      'Oturumunuz sona erdi. Lütfen tekrar giriş yapın.',
    );
  });
});
