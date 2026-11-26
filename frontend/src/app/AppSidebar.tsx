import { NavLink } from 'react-router';
import { useAuth } from '@/shared/auth/useAuth';
import { tr } from '@/shared/i18n/tr';
import { cn } from '@/shared/lib/utils';
import { navItems } from './navigation';

/** The main menu list, shared by the desktop sidebar and the mobile drawer; filtered by permission (spec 0002, R-11). */
export function AppSidebar({
  collapsed = false,
  onNavigate,
}: {
  collapsed?: boolean;
  onNavigate?: () => void;
}) {
  const { can } = useAuth();
  const visible = navItems.filter((item) => !item.permission || can(item.permission));
  return (
    <nav aria-label={tr.nav.label} className="flex-1 overflow-y-auto p-2">
      <ul className="flex flex-col gap-1">
        {visible.map(({ label, path, icon: Icon }) => (
          <li key={path}>
            <NavLink
              to={path}
              end
              title={collapsed ? label : undefined}
              onClick={onNavigate}
              className={({ isActive }) =>
                cn(
                  'flex h-9 items-center gap-3 rounded-md px-2.5 text-sm font-medium text-sidebar-foreground hover:bg-sidebar-accent/60',
                  collapsed && 'justify-center px-0',
                  isActive &&
                    'bg-sidebar-accent text-sidebar-accent-foreground hover:bg-sidebar-accent',
                )
              }
            >
              <Icon aria-hidden="true" className="size-4 shrink-0" data-testid="nav-icon" />
              <span className={cn('truncate', collapsed && 'sr-only')}>{label}</span>
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  );
}
