import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';

describe('index.html (spec 0001 AC-10)', () => {
  it('marks the page language as Turkish', () => {
    const html = readFileSync(resolve(process.cwd(), 'index.html'), 'utf-8');

    expect(html).toMatch(/<html[^>]*\blang="tr"/);
  });
});
