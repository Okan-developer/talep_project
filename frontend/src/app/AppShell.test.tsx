import { screen, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { renderApp } from '@/test/renderApp';
import { server, statusUrl } from '@/test/server';

describe('keyboard access (spec 0001 AC-10)', () => {
  it('Tab walks sidebar → top bar → content, reaching every menu item, the theme control and "Tekrar dene"', async () => {
    server.use(http.get(statusUrl, () => HttpResponse.error()));
    const { user } = renderApp('/');
    await screen.findByRole('alert');

    const menu = within(screen.getByRole('navigation', { name: 'Ana menü' })).getAllByRole('link');
    const expectedOrder = [
      ...menu,
      screen.getByRole('button', { name: 'Menüyü daralt' }),
      screen.getByRole('radio', { name: 'Sistem' }),
      screen.getByRole('button', { name: 'Tekrar dene' }),
    ];

    for (const element of expectedOrder) {
      await user.tab();
      expect(element).toHaveFocus();
    }
  });

  it('a focused menu item opens with Enter and "Tekrar dene" works with the keyboard', async () => {
    server.use(http.get(statusUrl, () => HttpResponse.error()));
    const { user, router } = renderApp('/');
    const retry = within(await screen.findByRole('alert')).getByRole('button', {
      name: 'Tekrar dene',
    });

    let calls = 0;
    server.use(
      http.get(statusUrl, () => {
        calls++;
        return HttpResponse.error();
      }),
    );
    retry.focus();
    await user.keyboard('{Enter}');
    await screen.findByRole('alert');
    expect(calls).toBe(1);

    screen.getByRole('link', { name: 'Taleplerim' }).focus();
    await user.keyboard('{Enter}');
    expect(router.state.location.pathname).toBe('/taleplerim');
  });
});
