import { render, screen, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { adminUser, refreshReturns, refreshUrl, server, userWith } from '@/test/server';
import { AuthProvider } from './AuthProvider';
import { getSession } from './session';
import { useAuth } from './useAuth';

function Probe() {
  const auth = useAuth();
  return (
    <p>
      {auth.status}|{auth.user?.fullName ?? '-'}|{auth.can('admin.users') ? 'admin' : 'no-admin'}
    </p>
  );
}

describe('AuthProvider (spec 0002, R-6 / AC-7)', () => {
  it('restores the session from the refresh cookie on start, without asking for login', async () => {
    server.use(refreshReturns(userWith(['Admin'], 'Ayşe Yılmaz')));

    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>,
    );

    expect(screen.getByText(/^restoring/)).toBeInTheDocument();
    expect(await screen.findByText('authenticated|Ayşe Yılmaz|admin')).toBeInTheDocument();
    expect(getSession()?.user.fullName).toBe('Ayşe Yılmaz');
  });

  it('is anonymous when there is no session to restore', async () => {
    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>,
    );

    expect(await screen.findByText('anonymous|-|no-admin')).toBeInTheDocument();
  });

  it('is anonymous when the server cannot be reached during restore', async () => {
    server.use(http.post(refreshUrl, () => HttpResponse.error()));

    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>,
    );

    await waitFor(() => expect(screen.getByText('anonymous|-|no-admin')).toBeInTheDocument());
  });

  it('restores once even when mounted twice (two tabs share the work through the lock)', async () => {
    let calls = 0;
    server.use(
      http.post(refreshUrl, async () => {
        calls += 1;
        await new Promise((resolve) => setTimeout(resolve, 10));
        return HttpResponse.json({
          accessToken: 't',
          expiresAt: new Date(Date.now() + 900_000).toISOString(),
          user: adminUser,
        });
      }),
    );

    render(
      <>
        <AuthProvider>
          <Probe />
        </AuthProvider>
        <AuthProvider>
          <Probe />
        </AuthProvider>
      </>,
    );

    expect(await screen.findAllByText(/^authenticated/)).toHaveLength(2);
    expect(calls).toBe(1);
  });
});
