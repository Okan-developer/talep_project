import { SearchX } from 'lucide-react';
import { Link } from 'react-router';
import { tr } from '@/shared/i18n/tr';
import { EmptyState } from '@/shared/ui/EmptyState';

export function NotFoundPage() {
  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-semibold tracking-tight">{tr.notFound.title}</h1>
      <EmptyState
        icon={SearchX}
        title={tr.notFound.title}
        description={tr.notFound.description}
        action={
          <Link
            to="/"
            className="text-sm font-medium text-primary underline-offset-4 hover:underline"
          >
            {tr.notFound.backHome}
          </Link>
        }
      />
    </div>
  );
}
