import { tr } from '@/shared/i18n/tr';
import { SystemStatusCard } from './SystemStatusCard';

export function OverviewPage() {
  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-semibold tracking-tight">{tr.nav.overview}</h1>
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        <SystemStatusCard />
      </div>
    </div>
  );
}
