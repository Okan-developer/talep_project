import { act, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { setMedia } from '@/test/media';
import { renderApp } from '@/test/renderApp';

const html = () => document.documentElement;

describe('theme (spec 0001 AC-8)', () => {
  it('starts on "Sistem" and follows the operating system preference', () => {
    renderApp('/');

    expect(screen.getByRole('radio', { name: 'Sistem' })).toHaveAttribute('aria-checked', 'true');
    expect(html()).not.toHaveClass('dark');

    act(() => setMedia({ dark: true }));
    expect(html()).toHaveClass('dark');

    act(() => setMedia({ dark: false }));
    expect(html()).not.toHaveClass('dark');
  });

  it('keeps the chosen theme after a reload', async () => {
    const first = renderApp('/');
    await first.user.click(screen.getByRole('radio', { name: 'Koyu' }));
    expect(html()).toHaveClass('dark');

    first.unmount();
    html().className = '';
    renderApp('/');

    expect(screen.getByRole('radio', { name: 'Koyu' })).toHaveAttribute('aria-checked', 'true');
    expect(html()).toHaveClass('dark');
  });

  it('"Açık" wins over a dark operating system', async () => {
    setMedia({ dark: true });
    const { user } = renderApp('/');
    expect(html()).toHaveClass('dark');

    await user.click(screen.getByRole('radio', { name: 'Açık' }));

    expect(html()).not.toHaveClass('dark');
  });

  it('arrow keys move the selection inside the theme group', async () => {
    const { user } = renderApp('/');
    screen.getByRole('radio', { name: 'Sistem' }).focus();

    await user.keyboard('{ArrowRight}');

    expect(screen.getByRole('radio', { name: 'Açık' })).toHaveAttribute('aria-checked', 'true');
    expect(screen.getByRole('radio', { name: 'Açık' })).toHaveFocus();
  });
});
