import fs from 'node:fs';
import js from '@eslint/js';
import { createTypeScriptImportResolver } from 'eslint-import-resolver-typescript';
import { importX } from 'eslint-plugin-import-x';
import reactHooks from 'eslint-plugin-react-hooks';
import { defineConfig } from 'eslint/config';
import globals from 'globals';
import tseslint from 'typescript-eslint';

// docs/architecture.md, Forbidden dependencies rule 6: a feature never imports another feature;
// src/shared imports neither features nor the app shell.
const features = fs
  .readdirSync('src/features', { withFileTypes: true })
  .filter((entry) => entry.isDirectory())
  .map((entry) => entry.name);

const zones = [
  {
    target: './src/shared',
    from: './src/features',
    message: 'src/shared bir feature içe aktaramaz (docs/architecture.md, kural 6).',
  },
  {
    target: './src/shared',
    from: './src/app',
    message: 'src/shared uygulama kabuğunu içe aktaramaz (docs/architecture.md, kural 6).',
  },
  ...features.map((feature) => ({
    target: `./src/features/${feature}`,
    from: './src/features',
    except: [`./${feature}`],
    message: 'Bir feature başka bir feature içe aktaramaz; ortak kodu src/shared altına taşıyın.',
  })),
];

export default defineConfig(
  { ignores: ['dist', 'coverage', 'src/shared/api/schema.d.ts'] },
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat.recommended,
    ],
    languageOptions: { globals: globals.browser },
    plugins: { 'import-x': importX },
    settings: { 'import-x/resolver-next': [createTypeScriptImportResolver()] },
    rules: {
      'import-x/no-restricted-paths': ['error', { zones }],
      'no-restricted-syntax': [
        'error',
        {
          selector: "JSXAttribute[name.name='dangerouslySetInnerHTML']",
          message: 'HTML render edilmez (docs/security.md).',
        },
      ],
    },
  },
);
