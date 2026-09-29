import { render } from '@testing-library/react';
import type { ReactElement } from 'react';
import { MemoryRouter } from 'react-router-dom';
import { vi } from 'vitest';
import type { AuthResult } from '../api/types';
import { AuthProvider } from '../auth/AuthProvider';
import { saveSession } from '../auth/session';

export const MEMBER_ID = '3f2b8a52-7a55-4d3e-9d55-0c6f0c1a0001';

export const memberSession: AuthResult = {
  token: 'member-token',
  expiresAt: '2099-01-01T00:00:00Z',
  userId: 'user-1',
  memberId: MEMBER_ID,
  role: 'Member',
};

export const adminSession: AuthResult = { ...memberSession, token: 'admin-token', memberId: null, role: 'Administrator' };

export interface FakeReply {
  status?: number;
  body?: unknown;
}

export type FakeHandler = (method: string, path: string, body: unknown) => FakeReply;

/** Replaces fetch with a handler and records each call as "METHOD /path". */
export function mockFetch(handler: FakeHandler) {
  const calls: { key: string; headers: Record<string, string>; body: unknown }[] = [];
  vi.stubGlobal(
    'fetch',
    vi.fn((input: string, init: RequestInit = {}) => {
      const method = init.method ?? 'GET';
      const path = String(input).replace(/^\/api/, '');
      const body: unknown = typeof init.body === 'string' ? JSON.parse(init.body) : undefined;
      calls.push({ key: `${method} ${path}`, headers: (init.headers ?? {}) as Record<string, string>, body });
      const reply = handler(method, path, body);
      const status = reply.status ?? 200;
      return Promise.resolve(
        status === 204 ? new Response(null, { status }) : new Response(JSON.stringify(reply.body ?? {}), { status }),
      );
    }),
  );
  return calls;
}

export function renderApp(ui: ReactElement, options: { route?: string; session?: AuthResult | null } = {}) {
  if (options.session) {
    saveSession(options.session);
  }
  return render(
    <MemoryRouter initialEntries={[options.route ?? '/']}>
      <AuthProvider>{ui}</AuthProvider>
    </MemoryRouter>,
  );
}
