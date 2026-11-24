import { cn } from '@/shared/lib/utils';

export type StatusTone = 'success' | 'danger' | 'neutral';

const toneClass: Record<StatusTone, string> = {
  success: 'text-success',
  danger: 'text-danger',
  neutral: 'text-muted-foreground',
};

const dotClass: Record<StatusTone, string> = {
  success: 'bg-success',
  danger: 'bg-danger',
  neutral: 'bg-muted-foreground',
};

/** Colour-coded status: the colour never carries meaning alone — the label is always shown. */
export function StatusIndicator({ tone, label }: { tone: StatusTone; label: string }) {
  return (
    <span
      className={cn('inline-flex items-center gap-2 font-medium', toneClass[tone])}
      data-tone={tone}
    >
      <span aria-hidden="true" className={cn('size-2 rounded-full', dotClass[tone])} />
      {label}
    </span>
  );
}
