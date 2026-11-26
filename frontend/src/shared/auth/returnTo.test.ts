import { describe, expect, it } from 'vitest';
import { loginPathFor, safeReturnTo } from './returnTo';

describe('safeReturnTo (spec 0002, R-8 / AC-10)', () => {
  it.each([
    ['/raporlar', '/raporlar'],
    ['/talepler?durum=acik', '/talepler?durum=acik'],
    ['/', '/'],
  ])('keeps the in-app address %s', (value, expected) => {
    expect(safeReturnTo(value)).toBe(expected);
  });

  it.each([
    [null],
    [''],
    ['https://kotu.example/'],
    ['//kotu.example/yol'],
    ['/\\kotu.example'],
    ['javascript:alert(1)'],
    ['raporlar'],
    ['/giris'],
    ['/giris?returnTo=/raporlar'],
    ['/a\nb'],
  ])('falls back to Genel Bakış for %j', (value) => {
    expect(safeReturnTo(value)).toBe('/');
  });

  it('builds the login address with the encoded return path', () => {
    expect(loginPathFor('/yonetim/kullanicilar')).toBe('/giris?returnTo=%2Fyonetim%2Fkullanicilar');
    expect(loginPathFor('/')).toBe('/giris');
  });
});
