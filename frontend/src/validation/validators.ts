export const MIN_PASSWORD_LENGTH = 12;

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const AMOUNT_PATTERN = /^\d+(\.\d{1,2})?$/;

/** Each validator returns an error message, or null when the value is acceptable. */
export function validateEmail(email: string): string | null {
  return EMAIL_PATTERN.test(email.trim()) ? null : 'Enter a valid email address.';
}

export function validatePassword(password: string): string | null {
  return password.length >= MIN_PASSWORD_LENGTH ? null : `Password must be at least ${MIN_PASSWORD_LENGTH} characters.`;
}

export function validateRequired(label: string, value: string): string | null {
  return value.trim() === '' ? `${label} is required.` : null;
}

export function validateAmount(value: string): string | null {
  const text = value.trim();
  const amount = Number(text);
  if (!Number.isFinite(amount) || amount <= 0) {
    return 'Enter an amount greater than zero.';
  }
  // Checked on the text: 19.99 * 100 is not exactly 1999 in floating point.
  return AMOUNT_PATTERN.test(text) ? null : 'Use at most two decimal places.';
}

/** Returns the first error among the given results, or null when all pass. */
export function firstError(...results: (string | null)[]): string | null {
  return results.find((result) => result !== null) ?? null;
}
