import { createContext, use, useEffect, useMemo, useState, type ReactNode } from 'react';
import { safeStorage } from '@/shared/lib/utils';
import { useMediaQuery } from './useMediaQuery';

export type Theme = 'light' | 'dark' | 'system';

export const THEME_STORAGE_KEY = 'talep.theme';

interface ThemeState {
  theme: Theme;
  setTheme: (theme: Theme) => void;
}

const ThemeContext = createContext<ThemeState | null>(null);

function readStoredTheme(): Theme {
  const stored = safeStorage.get(THEME_STORAGE_KEY);
  return stored === 'light' || stored === 'dark' || stored === 'system' ? stored : 'system';
}

/** Light / Dark / System (spec 0001, R-6): remembered in the browser, System follows the OS. */
export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setThemeState] = useState<Theme>(readStoredTheme);
  const systemPrefersDark = useMediaQuery('(prefers-color-scheme: dark)');
  const isDark = theme === 'dark' || (theme === 'system' && systemPrefersDark);

  useEffect(() => {
    document.documentElement.classList.toggle('dark', isDark);
    document.documentElement.style.colorScheme = isDark ? 'dark' : 'light';
  }, [isDark]);

  const value = useMemo<ThemeState>(
    () => ({
      theme,
      setTheme: (next) => {
        safeStorage.set(THEME_STORAGE_KEY, next);
        setThemeState(next);
      },
    }),
    [theme],
  );

  return <ThemeContext value={value}>{children}</ThemeContext>;
}

export function useTheme(): ThemeState {
  const state = use(ThemeContext);
  if (!state) throw new Error('useTheme must be used inside ThemeProvider');
  return state;
}
