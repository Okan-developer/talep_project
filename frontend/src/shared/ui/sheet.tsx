import { Dialog } from 'radix-ui';
import type { ComponentProps, ReactNode } from 'react';
import { cn } from '@/shared/lib/utils';

export const Sheet = Dialog.Root;
export const SheetTrigger = Dialog.Trigger;

/** A left-side drawer (mobile navigation). Esc and the overlay close it (Radix Dialog). */
export function SheetContent({
  className,
  title,
  children,
  ...props
}: ComponentProps<typeof Dialog.Content> & { title: string; children: ReactNode }) {
  return (
    <Dialog.Portal>
      <Dialog.Overlay className="fixed inset-0 z-40 bg-black/50" />
      <Dialog.Content
        aria-describedby={undefined}
        className={cn(
          'fixed inset-y-0 left-0 z-50 flex w-72 flex-col bg-sidebar text-sidebar-foreground shadow-lg',
          className,
        )}
        {...props}
      >
        <Dialog.Title className="sr-only">{title}</Dialog.Title>
        {children}
      </Dialog.Content>
    </Dialog.Portal>
  );
}
