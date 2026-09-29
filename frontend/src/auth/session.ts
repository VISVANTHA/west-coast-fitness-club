import type { AuthResult } from '../api/types';

const STORAGE_KEY = 'wcf.session';

function isSession(value: Partial<AuthResult>, now: Date): value is AuthResult {
  return (
    typeof value.token === 'string' &&
    typeof value.expiresAt === 'string' &&
    (value.role === 'Member' || value.role === 'Administrator') &&
    new Date(value.expiresAt) > now
  );
}

/** Reads the stored session, discarding anything malformed or expired. */
export function loadSession(now: Date = new Date()): AuthResult | null {
  const raw = sessionStorage.getItem(STORAGE_KEY);
  if (raw === null) {
    return null;
  }

  try {
    const parsed = JSON.parse(raw) as Partial<AuthResult>;
    if (isSession(parsed, now)) {
      return parsed;
    }
  } catch {
    // Unparseable data is treated the same as an invalid session.
  }
  sessionStorage.removeItem(STORAGE_KEY);
  return null;
}

export function saveSession(session: AuthResult): void {
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
}

export function clearSession(): void {
  sessionStorage.removeItem(STORAGE_KEY);
}

export function getToken(): string | null {
  return loadSession()?.token ?? null;
}
