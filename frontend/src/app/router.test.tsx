import { screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { navItems } from '@/app/navigation';
import { renderApp } from '@/test/renderApp';

describe('routing (spec 0001 AC-2)', () => {
  it.each(navItems.filter((item) => item.path !== '/').map((item) => [item.label]))(
    'menu item "%s" opens its "Bu ekran hazırlanıyor" page and is highlighted',
    async (label) => {
      const { user } = renderApp('/');

      await user.click(screen.getByRole('link', { name: label }));

      expect(screen.getByRole('heading', { level: 1, name: label })).toBeInTheDocument();
      expect(screen.getByText('Bu ekran hazırlanıyor')).toBeInTheDocument();
      expect(screen.getByRole('link', { name: label })).toHaveAttribute('aria-current', 'page');
      expect(screen.getByRole('link', { name: 'Genel Bakış' })).not.toHaveAttribute('aria-current');
    },
  );

  it('an unknown address shows "Sayfa bulunamadı" with a way back to Genel Bakış', async () => {
    const { user, router } = renderApp('/boyle-bir-sayfa-yok');

    expect(screen.getByRole('heading', { level: 1, name: 'Sayfa bulunamadı' })).toBeInTheDocument();
    await user.click(screen.getByRole('link', { name: 'Genel Bakış’a dön' }));

    expect(router.state.location.pathname).toBe('/');
    expect(screen.getByRole('heading', { level: 1, name: 'Genel Bakış' })).toBeInTheDocument();
  });
});
