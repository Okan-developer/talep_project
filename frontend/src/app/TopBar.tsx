import { LogOut, UserRound } from 'lucide-react';
import { useNavigate } from 'react-router';
import { LOGIN_PATH } from '@/shared/auth/returnTo';
import { useAuth } from '@/shared/auth/useAuth';
import { tr } from '@/shared/i18n/tr';
import { Button } from '@/shared/ui/button';
import { ThemeToggle } from './ThemeToggle';

/** The right side of the top bar: theme, the signed-in user's name and "Çıkış" (spec 0002, R-7). */
export function TopBar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const signOut = async () => {
    await logout();
    void navigate(LOGIN_PATH, { replace: true });
  };

  return (
    <div className="ml-auto flex min-w-0 items-center gap-2">
      <ThemeToggle />
      {user && (
        <p className="flex min-w-0 items-center gap-1.5 text-sm">
          <UserRound aria-hidden="true" className="size-4 shrink-0 text-muted-foreground" />
          <span className="sr-only">{tr.shell.signedInAs}: </span>
          <span className="truncate font-medium">{user.fullName}</span>
        </p>
      )}
      <Button variant="ghost" size="sm" onClick={() => void signOut()}>
        <LogOut aria-hidden="true" />
        {tr.shell.logout}
      </Button>
    </div>
  );
}
