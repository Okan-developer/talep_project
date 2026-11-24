/**
 * A controllable window.matchMedia for jsdom: tests decide the OS colour scheme and the viewport
 * (desktop / below 768 px) and can change them while rendered.
 */
type Listener = (event: MediaQueryListEvent) => void;

const state = { dark: false, mobile: false };
const lists = new Set<{ query: string; listeners: Set<Listener> }>();

function evaluate(query: string): boolean {
  if (query.includes('prefers-color-scheme: dark')) return state.dark;
  if (query.includes('max-width: 767px')) return state.mobile;
  if (query.includes('prefers-reduced-motion: reduce')) return true;
  return false;
}

export function installMatchMedia() {
  window.matchMedia = (query: string): MediaQueryList => {
    const entry = { query, listeners: new Set<Listener>() };
    lists.add(entry);
    return {
      get matches() {
        return evaluate(query);
      },
      media: query,
      onchange: null,
      addEventListener: (_type: string, listener: Listener) => entry.listeners.add(listener),
      removeEventListener: (_type: string, listener: Listener) => entry.listeners.delete(listener),
      addListener: (listener: Listener) => entry.listeners.add(listener),
      removeListener: (listener: Listener) => entry.listeners.delete(listener),
      dispatchEvent: () => true,
    } as unknown as MediaQueryList;
  };
}

export function setMedia(next: Partial<typeof state>) {
  Object.assign(state, next);
  for (const entry of lists) {
    const event = { matches: evaluate(entry.query), media: entry.query } as MediaQueryListEvent;
    entry.listeners.forEach((listener) => listener(event));
  }
}

export function resetMedia() {
  state.dark = false;
  state.mobile = false;
  lists.clear();
}
