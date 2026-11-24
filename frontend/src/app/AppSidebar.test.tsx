import { screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { setMedia } from '@/test/media';
import { renderApp } from '@/test/renderApp';

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
