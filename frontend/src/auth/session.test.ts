import { describe, expect, it } from 'vitest';
import { clearSession, getToken, loadSession, saveSession } from './session';
import { memberSession } from '../test/helpers';

describe('session storage', () => {
  it('round-trips a valid session', () => {
    saveSession(memberSession);

    expect(loadSession()).toEqual(memberSession);
    expect(getToken()).toBe('member-token');
  });

  it('returns null when nothing is stored', () => {
    expect(loadSession()).toBeNull();
    expect(getToken()).toBeNull();
  });

  it('discards expired sessions', () => {
    saveSession({ ...memberSession, expiresAt: '2001-01-01T00:00:00Z' });

    expect(loadSession()).toBeNull();
    expect(sessionStorage.getItem('wcf.session')).toBeNull();
  });

  it('discards malformed or incomplete data', () => {
    sessionStorage.setItem('wcf.session', '{not json');
    expect(loadSession()).toBeNull();

    sessionStorage.setItem('wcf.session', JSON.stringify({ token: 'x', expiresAt: '2099-01-01T00:00:00Z', role: 'Hacker' }));
    expect(loadSession()).toBeNull();
    expect(sessionStorage.getItem('wcf.session')).toBeNull();
  });

  it('clears the session', () => {
    saveSession(memberSession);
    clearSession();

    expect(loadSession()).toBeNull();
  });
});
