import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { App } from './App';
import { useAuth } from './auth/useAuth';
import { memberSession, mockFetch, renderApp } from './test/helpers';

describe('App', () => {
  it('redirects the root to the plans page and shows anonymous navigation', async () => {
    mockFetch(() => ({ body: [] }));
    renderApp(<App />, { route: '/' });

    expect(await screen.findByRole('heading', { name: 'Membership plans' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Sign in' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Join' })).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'My bookings' })).toBeNull();
  });

  it('shows member navigation and signs out', async () => {
    mockFetch(() => ({ body: [] }));
    renderApp(<App />, { route: '/classes', session: memberSession });

    expect(await screen.findByRole('link', { name: 'My bookings' })).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Sign out' }));

    expect(await screen.findByRole('heading', { name: 'Sign in' })).toBeInTheDocument();
    expect(sessionStorage.getItem('wcf.session')).toBeNull();
  });

  it('renders a not-found page for unknown routes', () => {
    mockFetch(() => ({ body: [] }));
    renderApp(<App />, { route: '/nowhere' });

    expect(screen.getByText('Page not found.')).toBeInTheDocument();
  });
});

describe('useAuth', () => {
  it('refuses to run outside an AuthProvider', () => {
    function Probe() {
      useAuth();
      return null;
    }

    expect(() => render(<Probe />)).toThrow('useAuth must be used inside an AuthProvider.');
  });
});
