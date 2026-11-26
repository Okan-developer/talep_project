import { ShieldX } from 'lucide-react';
import { Link } from 'react-router';
import { tr } from '@/shared/i18n/tr';
import { EmptyState } from '@/shared/ui/EmptyState';

/** "Yetki yok" (spec 0002, R-11 / AC-15): the shell and menu keep working around it. */
export function ForbiddenPage() {
  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-semibold tracking-tight">{tr.forbidden.title}</h1>
      <EmptyState
        icon={ShieldX}
        title={tr.forbidden.description}
        description={tr.forbidden.hint}
        action={
          <Link
            to="/"
            className="text-sm font-medium text-primary underline-offset-4 hover:underline"
          >
            {tr.forbidden.backHome}
          </Link>
        }
      />
    </div>
  );
}
