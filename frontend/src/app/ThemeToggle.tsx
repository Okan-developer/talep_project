import { Monitor, Moon, Sun, type LucideIcon } from 'lucide-react';
import type { KeyboardEvent } from 'react';
import { tr } from '@/shared/i18n/tr';
import { cn } from '@/shared/lib/utils';
import { useTheme, type Theme } from './ThemeProvider';

const options: { value: Theme; label: string; icon: LucideIcon }[] = [
  { value: 'light', label: tr.theme.light, icon: Sun },
  { value: 'dark', label: tr.theme.dark, icon: Moon },
  { value: 'system', label: tr.theme.system, icon: Monitor },
];

/** A radio group: Tab reaches the selected option, arrow keys move between options. */
export function ThemeToggle() {
  const { theme, setTheme } = useTheme();

  const onKeyDown = (event: KeyboardEvent<HTMLButtonElement>, index: number) => {
    const step =
      event.key === 'ArrowRight' || event.key === 'ArrowDown'
        ? 1
        : event.key === 'ArrowLeft' || event.key === 'ArrowUp'
          ? -1
          : 0;
    if (step === 0) return;
    event.preventDefault();
    const next = options[(index + step + options.length) % options.length]!;
    setTheme(next.value);
    const group = event.currentTarget.parentElement;
    group?.querySelector<HTMLButtonElement>(`[data-value="${next.value}"]`)?.focus();
  };

  return (
    <div
      role="radiogroup"
      aria-label={tr.theme.label}
      className="inline-flex rounded-md border p-0.5"
    >
      {options.map(({ value, label, icon: Icon }, index) => {
        const selected = theme === value;
        return (
          <button
            key={value}
            type="button"
            role="radio"
            aria-checked={selected}
            aria-label={label}
            title={label}
            data-value={value}
            tabIndex={selected ? 0 : -1}
            onClick={() => setTheme(value)}
            onKeyDown={(event) => onKeyDown(event, index)}
            className={cn(
              'inline-flex size-8 items-center justify-center rounded-sm text-muted-foreground hover:text-foreground',
              selected && 'bg-secondary text-secondary-foreground',
            )}
          >
            <Icon aria-hidden="true" className="size-4" />
          </button>
        );
      })}
    </div>
  );
}
