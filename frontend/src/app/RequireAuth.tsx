import type { ReactNode } from 'react';
import { Navigate, Outlet, useLocation, useSearchParams } from 'react-router';
import { loginPathFor, safeReturnTo } from '@/shared/auth/returnTo';
import { useAuth } from '@/shared/auth/useAuth';
import { FullPageLoader } from './FullPageLoader';

/** Protected routes: a signed-out visitor goes to the login screen and comes back afterwards (spec 0002, R-8). */
export function RequireAuth() {
  const { status } = useAuth();
  const location = useLocation();

  if (status === 'restoring') return <FullPageLoader />;
  if (status === 'anonymous') {
    return <Navigate to={loginPathFor(location.pathname + location.search)} replace />;
  }
  return <Outlet />;
}

/** The login route: a signed-in user is sent on to the return address instead. */
export function GuestOnly({ children }: { children: ReactNode }) {
  const { status } = useAuth();
  const [params] = useSearchParams();

  if (status === 'restoring') return <FullPageLoader />;
  if (status === 'authenticated') {
    return <Navigate to={safeReturnTo(params.get('returnTo'))} replace />;
  }
  return children;
}
