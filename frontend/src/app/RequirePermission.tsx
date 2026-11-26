import type { ReactNode } from 'react';
import { useAuth } from '@/shared/auth/useAuth';
import { ForbiddenPage } from './pages/ForbiddenPage';

/** A page the menu hides from users without the permission; opened by address it says so (spec 0002, R-11). */
export function RequirePermission({
  permission,
  children,
}: {
  permission?: string;
  children: ReactNode;
}) {
  const { can } = useAuth();
  return !permission || can(permission) ? children : <ForbiddenPage />;
}
