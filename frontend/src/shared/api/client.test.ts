import { afterEach, describe, expect, it, vi } from 'vitest';
import { REQUEST_TIMEOUT_MS, withTimeout } from './client';

describe('withTimeout (spec 0001 AC-4: 10 seconds)', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('the request timeout is 10 seconds', () => {
    expect(REQUEST_TIMEOUT_MS).toBe(10_000);
  });

  it('aborts exactly when the timeout elapses and settles a request that never answers', async () => {
    vi.useFakeTimers();
    const timeout = withTimeout(undefined);
    const outcome = timeout.race(new Promise<never>(() => {})).then(
      () => 'resolved',
      (error: unknown) => (error as DOMException).name,
    );

    vi.advanceTimersByTime(REQUEST_TIMEOUT_MS - 1);
    expect(timeout.signal.aborted).toBe(false);

    vi.advanceTimersByTime(1);
    expect(timeout.signal.aborted).toBe(true);
    await expect(outcome).resolves.toBe('TimeoutError');
  });

  it('follows the caller abort and stops the timer on dispose', () => {
    vi.useFakeTimers();
    const caller = new AbortController();
    const timeout = withTimeout(caller.signal);

    caller.abort();
    expect(timeout.signal.aborted).toBe(true);

    const other = withTimeout(undefined);
    other.dispose();
    vi.advanceTimersByTime(REQUEST_TIMEOUT_MS);
    expect(other.signal.aborted).toBe(false);
  });
});
