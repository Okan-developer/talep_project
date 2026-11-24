import type { RouteObject } from 'react-router';
import { OverviewPage } from '@/features/overview/OverviewPage';
import { AppShell } from './AppShell';
import { navItems } from './navigation';
import { ComingSoonPage } from './pages/ComingSoonPage';
import { NotFoundPage } from './pages/NotFoundPage';

export const routes: RouteObject[] = [
  {
    path: '/',
    element: <AppShell />,
    children: [
      { index: true, element: <OverviewPage /> },
      ...navItems
        .filter((item) => item.path !== '/')
        .map((item) => ({ path: item.path, element: <ComingSoonPage title={item.label} /> })),
      { path: '*', element: <NotFoundPage /> },
    ],
  },
];
