import { LoaderCircle } from 'lucide-react';
import { tr } from '@/shared/i18n/tr';

/** Shown while the session is restored, so neither the login screen nor protected content flashes (spec 0002). */
export function FullPageLoader() {
  return (
    <div
      role="status"
      aria-label={tr.auth.restoring}
      className="flex min-h-svh items-center justify-center bg-background text-muted-foreground"
    >
      <LoaderCircle aria-hidden="true" className="size-8 animate-spin" />
    </div>
  );
}
