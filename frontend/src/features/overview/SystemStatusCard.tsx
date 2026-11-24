import type { ReactNode } from 'react';
import type { SystemStatus } from '@/shared/api/client';
import { tr } from '@/shared/i18n/tr';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card';
import { ErrorState } from '@/shared/ui/ErrorState';
import { Skeleton } from '@/shared/ui/skeleton';
import { StatusIndicator } from '@/shared/ui/StatusIndicator';
import { useSystemStatus } from './useSystemStatus';

const t = tr.systemStatus;

export function SystemStatusCard() {
  const query = useSystemStatus();
  const isLoading = query.fetchStatus === 'fetching' && !query.data;

  let body: ReactNode;
  if (isLoading) {
    body = (
      <div role="status" aria-label={t.loading} className="flex flex-col gap-3">
        <Skeleton className="h-5 w-40" />
        <Skeleton className="h-5 w-48" />
        <Skeleton className="h-5 w-36" />
      </div>
    );
  } else if (query.isError || !query.data) {
    body = (
      <ErrorState message={t.error} retryLabel={t.retry} onRetry={() => void query.refetch()} />
    );
  } else {
    body = <StatusRows status={query.data} />;
  }

  return (
    <Card aria-labelledby="system-status-title">
      <CardHeader>
        <CardTitle id="system-status-title">{t.title}</CardTitle>
      </CardHeader>
      <CardContent>{body}</CardContent>
    </Card>
  );
}

function StatusRows({ status }: { status: SystemStatus }) {
  const databaseUp = status.database === 'available';
  let departments: ReactNode;
  if (!databaseUp || status.departmentCount === null) {
    departments = <span className="font-medium">{t.notAvailable}</span>;
  } else if (status.departmentCount === 0) {
    departments = <StatusIndicator tone="neutral" label={t.noDepartments} />;
  } else {
    departments = <span className="font-medium">{status.departmentCount}</span>;
  }

  return (
    <dl className="grid grid-cols-[auto_1fr] gap-x-6 gap-y-3 text-sm">
      <dt className="text-muted-foreground">{t.api}</dt>
      <dd>
        <StatusIndicator tone="success" label={t.apiUp} />
      </dd>
      <dt className="text-muted-foreground">{t.database}</dt>
      <dd>
        <StatusIndicator
          tone={databaseUp ? 'success' : 'danger'}
          label={databaseUp ? t.databaseAvailable : t.databaseUnavailable}
        />
      </dd>
      <dt className="text-muted-foreground">{t.departmentCount}</dt>
      <dd>{departments}</dd>
    </dl>
  );
}
