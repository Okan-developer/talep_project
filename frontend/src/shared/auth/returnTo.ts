export const LOGIN_PATH = '/giris';

/**
 * The in-app address to go to after login (spec 0002, R-8 / AC-10): only a path inside the
 * application; anything else — another origin, a protocol-relative or backslash trick, the login
 * page itself — falls back to Genel Bakış.
 */
export function safeReturnTo(value: string | null | undefined): string {
  if (!value || !value.startsWith('/')) return '/';
  if (value.startsWith('//') || value.startsWith('/\\')) return '/';
  if ([...value].some((c) => c.charCodeAt(0) < 0x20)) return '/';
  if (value === LOGIN_PATH || value.startsWith(`${LOGIN_PATH}?`)) return '/';
  return value;
}

/** The login address that comes back to `path` after login. */
export function loginPathFor(path: string): string {
  const target = safeReturnTo(path);
  return target === '/' ? LOGIN_PATH : `${LOGIN_PATH}?returnTo=${encodeURIComponent(target)}`;
}
