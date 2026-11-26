import { screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { setMedia } from '@/test/media';
import { renderApp } from '@/test/renderApp';
import { userWith } from '@/test/server';

const expectedMenu = [
  'Genel Bakış',
  'Taleplerim',
  'Yeni Talep Oluştur',
  'Onay Bekleyenler',
  'Birim Talepleri',
  'Bana Atanan İşler',
  'Tüm Talepler',
  'Raporlar',
  'Kullanıcı Yönetimi',
  'Kategori Yönetimi',
  'Onay Akışı Yönetimi',
  'Ayarlar',
];

function menuLinks() {
  return within(screen.getByRole('navigation', { name: 'Ana menü' })).getAllByRole('link');
}

describe('AppSidebar (spec 0001 AC-1, AC-9)', () => {
  it('shows the 12 menu items in order with Turkish names and icons, Genel Bakış active by default', async () => {
    renderApp('/');

    const links = menuLinks();
    expect(links.map((link) => link.textContent)).toEqual(expectedMenu);
    for (const link of links) {
      expect(link.querySelector('svg')).not.toBeNull();
    }
    expect(screen.getByRole('link', { name: 'Genel Bakış' })).toHaveAttribute(
      'aria-current',
      'page',
    );
    expect(
      await screen.findByRole('heading', { level: 1, name: 'Genel Bakış' }),
    ).toBeInTheDocument();
  });

  it('collapses to icons on desktop, keeps an accessible name per icon, and remembers it after reload', async () => {
    const first = renderApp('/');
    await first.user.click(screen.getByRole('button', { name: 'Menüyü daralt' }));

    expect(screen.getByRole('complementary')).toHaveAttribute('data-collapsed', 'true');
    expect(menuLinks().map((link) => link.textContent)).toEqual(expectedMenu); // names still exposed
    for (const label of within(screen.getByRole('navigation')).getAllByText(/.+/)) {
      expect(label).toHaveClass('sr-only');
    }
    expect(screen.getByRole('button', { name: 'Menüyü genişlet' })).toHaveAttribute(
      'aria-expanded',
      'false',
    );

    first.unmount();
    renderApp('/');
    expect(screen.getByRole('complementary')).toHaveAttribute('data-collapsed', 'true');
  });

  it('below 768 px the menu is a drawer that closes on selection and on Escape', async () => {
    setMedia({ mobile: true });
    const { user, router } = renderApp('/');

    expect(screen.queryByRole('navigation', { name: 'Ana menü' })).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Menüyü aç' }));
    const drawer = screen.getByRole('dialog', { name: 'Ana menü' });
    await user.click(within(drawer).getByRole('link', { name: 'Raporlar' }));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(router.state.location.pathname).toBe('/raporlar');

    await user.click(screen.getByRole('button', { name: 'Menüyü aç' }));
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    await user.keyboard('{Escape}');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });
});

describe('role-based menu (spec 0002, R-11 / AC-14)', () => {
  const everyone = ['Genel Bakış', 'Taleplerim', 'Yeni Talep Oluştur'];
  const unitManager = [
    'Onay Bekleyenler',
    'Birim Talepleri',
    'Bana Atanan İşler',
    'Tüm Talepler',
    'Raporlar',
  ];
  const table: [name: string, roles: string[], extra: string[]][] = [
    ['Talep Sahibi', ['Requester'], []],
    ['Birim Yöneticisi', ['DepartmentManager'], ['Onay Bekleyenler']],
    ['BT Yazılım Personeli', ['SoftwareStaff'], ['Bana Atanan İşler']],
    ['BT Sistem Personeli', ['SystemStaff'], ['Bana Atanan İşler']],
    ['BT Yazılım Birim Yöneticisi', ['ItUnitManager'], unitManager],
    ['BT Sistem Birim Yöneticisi', ['ItUnitManager'], unitManager],
    [
      'Sistem Yöneticisi',
      ['Admin'],
      [
        'Onay Bekleyenler',
        'Birim Talepleri',
        'Tüm Talepler',
        'Raporlar',
        'Kullanıcı Yönetimi',
        'Kategori Yönetimi',
        'Onay Akışı Yönetimi',
        'Ayarlar',
      ],
    ],
  ];

  it.each(table)('%s sees exactly the R-11 items, in the spec 0001 order', (name, roles, extra) => {
    renderApp('/', { user: userWith(roles, name) });

    const expected = expectedMenu.filter((item) => everyone.includes(item) || extra.includes(item));
    expect(menuLinks().map((link) => link.textContent)).toEqual(expected);
  });

  it('a user with two roles sees the union of both', () => {
    renderApp('/', { user: userWith(['DepartmentManager', 'SoftwareStaff'], 'İki Rollü') });

    expect(menuLinks().map((link) => link.textContent)).toEqual([
      ...everyone,
      'Onay Bekleyenler',
      'Bana Atanan İşler',
    ]);
  });
});
