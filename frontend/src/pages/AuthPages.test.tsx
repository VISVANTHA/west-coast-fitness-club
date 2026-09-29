import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Route, Routes } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { LoginPage } from './LoginPage';
import { RegisterPage } from './RegisterPage';
import { loadSession } from '../auth/session';
import { memberSession, mockFetch, renderApp } from '../test/helpers';

function LoginRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/classes" element={<p>classes page</p>} />
    </Routes>
  );
}

describe('LoginPage', () => {
  it('signs in and moves on to the classes page', async () => {
    const calls = mockFetch(() => ({ body: memberSession }));
    renderApp(<LoginRoutes />, { route: '/login' });

    await userEvent.type(screen.getByLabelText('Email'), 'ana@example.com');
    await userEvent.type(screen.getByLabelText('Password'), 'correct-horse-battery');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByText('classes page')).toBeInTheDocument();
    expect(calls[0]?.key).toBe('POST /auth/login');
    expect(loadSession()?.token).toBe('member-token');
  });

  it('validates before calling the API', async () => {
    const calls = mockFetch(() => ({}));
    renderApp(<LoginRoutes />, { route: '/login' });

    await userEvent.type(screen.getByLabelText('Email'), 'not-an-email');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(screen.getByRole('alert')).toHaveTextContent('valid email');
    expect(calls).toHaveLength(0);
  });

  it('requires a password', async () => {
    mockFetch(() => ({}));
    renderApp(<LoginRoutes />, { route: '/login' });

    await userEvent.type(screen.getByLabelText('Email'), 'ana@example.com');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(screen.getByRole('alert')).toHaveTextContent('Password is required');
  });

  it('shows the API error and re-enables the form', async () => {
    mockFetch(() => ({ status: 401, body: { detail: 'Email or password is incorrect.' } }));
    renderApp(<LoginRoutes />, { route: '/login' });

    await userEvent.type(screen.getByLabelText('Email'), 'ana@example.com');
    await userEvent.type(screen.getByLabelText('Password'), 'wrong-password');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Email or password is incorrect.');
    await waitFor(() => expect(screen.getByRole('button', { name: 'Sign in' })).toBeEnabled());
    expect(loadSession()).toBeNull();
  });
});

describe('RegisterPage', () => {
  function RegisterRoutes() {
    return (
      <Routes>
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/plans" element={<p>plans page</p>} />
      </Routes>
    );
  }

  async function fill(values: Partial<Record<string, string>>) {
    for (const [label, value] of Object.entries(values)) {
      await userEvent.type(screen.getByLabelText(label), value ?? '');
    }
  }

  it('creates an account and signs the member in', async () => {
    const calls = mockFetch(() => ({ status: 201, body: memberSession }));
    renderApp(<RegisterRoutes />, { route: '/register' });

    await fill({
      'First name': 'Ana',
      'Last name': 'Reyes',
      Email: 'ana@example.com',
      Phone: '602-555-0100',
      Password: 'correct-horse-battery',
    });
    await userEvent.click(screen.getByRole('button', { name: 'Create account' }));

    expect(await screen.findByText('plans page')).toBeInTheDocument();
    expect(calls[0]?.body).toMatchObject({ firstName: 'Ana', lastName: 'Reyes', email: 'ana@example.com' });
  });

  it.each([
    [{ 'Last name': 'Reyes', Email: 'a@b.co', Password: 'correct-horse-battery' }, 'First name is required'],
    [{ 'First name': 'Ana', Email: 'a@b.co', Password: 'correct-horse-battery' }, 'Last name is required'],
    [{ 'First name': 'Ana', 'Last name': 'Reyes', Email: 'bad', Password: 'correct-horse-battery' }, 'valid email'],
    [{ 'First name': 'Ana', 'Last name': 'Reyes', Email: 'a@b.co', Password: 'short' }, 'at least 12'],
  ])('rejects invalid input %#', async (values, expected) => {
    const calls = mockFetch(() => ({}));
    renderApp(<RegisterRoutes />, { route: '/register' });

    await fill(values);
    await userEvent.click(screen.getByRole('button', { name: 'Create account' }));

    expect(screen.getByRole('alert')).toHaveTextContent(expected);
    expect(calls).toHaveLength(0);
  });

  it('surfaces a duplicate-email conflict', async () => {
    mockFetch(() => ({ status: 409, body: { detail: 'An account with this email already exists.' } }));
    renderApp(<RegisterRoutes />, { route: '/register' });

    await fill({ 'First name': 'Ana', 'Last name': 'Reyes', Email: 'a@b.co', Password: 'correct-horse-battery' });
    await userEvent.click(screen.getByRole('button', { name: 'Create account' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('already exists');
  });
});
