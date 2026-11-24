import { mergeConfig, defineConfig } from 'vitest/config';
import viteConfig from './vite.config';

// Determinism (docs/testing.md): fixed time zone and locale for every test run.
process.env.TZ = 'Europe/Istanbul';
process.env.LANG = 'tr_TR.UTF-8';

export default mergeConfig(
  viteConfig,
  defineConfig({
    test: {
      environment: 'jsdom',
      setupFiles: ['./src/test/setup.ts'],
      restoreMocks: true,
      // resource hygiene: `vitest run` only (package.json "test"), never watch mode in check
      watch: false,
    },
  }),
);
