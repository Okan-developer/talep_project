import { Construction } from 'lucide-react';
import { tr } from '@/shared/i18n/tr';
import { EmptyState } from '@/shared/ui/EmptyState';

export function ComingSoonPage({ title }: { title: string }) {
  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-semibold tracking-tight">{title}</h1>
      <EmptyState
        icon={Construction}
        title={tr.comingSoon.title}
        description={tr.comingSoon.description}
      />
    </div>
  );
}
