import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterAll, afterEach, beforeAll, beforeEach } from 'vitest';
import { resetSessionForTests } from '@/shared/auth/session';
import { installMatchMedia, resetMedia } from './media';
import { server } from './server';

beforeAll(() => server.listen({ onUnhandledFrame: 'error' }));

beforeEach(() => {
  resetMedia();
  installMatchMedia();
});

afterEach(() => {
  cleanup();
  server.resetHandlers();
  resetSessionForTests();
  window.localStorage.clear();
  document.documentElement.className = '';
  document.documentElement.removeAttribute('style');
});

afterAll(() => server.close());
