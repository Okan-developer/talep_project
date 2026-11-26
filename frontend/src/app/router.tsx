import type { RouteObject } from 'react-router';
import { LoginPage } from '@/features/auth/LoginPage';
import { OverviewPage } from '@/features/overview/OverviewPage';
import { LOGIN_PATH } from '@/shared/auth/returnTo';
import { AppShell } from './AppShell';
import { navItems } from './navigation';
import { ComingSoonPage } from './pages/ComingSoonPage';
import { NotFoundPage } from './pages/NotFoundPage';
import { GuestOnly, RequireAuth } from './RequireAuth';
import { RequirePermission } from './RequirePermission';

export const routes: RouteObject[] = [
  {
    path: LOGIN_PATH,
    element: (
      <GuestOnly>
        <LoginPage />
      </GuestOnly>
    ),
  },
  {
    path: '/',
    element: <RequireAuth />,
    children: [
      {
        element: <AppShell />,
        children: [
          { index: true, element: <OverviewPage /> },
          ...navItems
            .filter((item) => item.path !== '/')
            .map((item) => ({
              path: item.path,
              element: (
                <RequirePermission permission={item.permission}>
                  <ComingSoonPage title={item.label} />
                </RequirePermission>
              ),
            })),
          { path: '*', element: <NotFoundPage /> },
        ],
      },
    ],
  },
];
