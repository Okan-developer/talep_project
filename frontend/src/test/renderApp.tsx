import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { routes } from '@/app/router';
import { ThemeProvider } from '@/app/ThemeProvider';
import type { CurrentUser } from '@/shared/api/client';
import { AuthProvider } from '@/shared/auth/AuthProvider';
import { setSession } from '@/shared/auth/session';
import { adminUser, sessionFor } from './server';

/**
 * Renders the whole application at a path, as main.tsx does, with an in-memory router.
 * By default a Sistem Yöneticisi is already signed in (all 12 menu items); pass `user: null` to
 * start signed out — the app then restores the session from the (stubbed) refresh endpoint.
 */
export function renderApp(
  path = '/',
  options: { userEvent?: Parameters<typeof userEvent.setup>[0]; user?: CurrentUser | null } = {},
) {
  const signedIn = options.user === undefined ? adminUser : options.user;
  if (signedIn) setSession(sessionFor(signedIn));
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false, refetchOnWindowFocus: false } },
  });
  const router = createMemoryRouter(routes, { initialEntries: [path] });
  const user = userEvent.setup(options.userEvent);
  const view = render(
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>
        <AuthProvider>
          <RouterProvider router={router} />
        </AuthProvider>
      </ThemeProvider>
    </QueryClientProvider>,
  );
  return { ...view, user, router };
}
