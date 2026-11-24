import { CircleAlert, RefreshCw } from 'lucide-react';
import { Button } from './button';

export function ErrorState({
  message,
  retryLabel,
  onRetry,
}: {
  message: string;
  retryLabel: string;
  onRetry: () => void;
}) {
  return (
    <div role="alert" className="flex flex-col items-start gap-3">
      <p className="flex items-center gap-2 text-sm text-danger">
        <CircleAlert aria-hidden="true" className="size-4" />
        {message}
      </p>
      <Button variant="outline" size="sm" onClick={onRetry}>
        <RefreshCw aria-hidden="true" />
        {retryLabel}
      </Button>
    </div>
  );
}
