import { describe, expect, it } from 'vitest';
import { formatCurrency, formatDate, formatDateTime, formatWeeklyLimit } from './format';

describe('format', () => {
  it('formats US dollars', () => {
    expect(formatCurrency(29.99)).toBe('$29.99');
    expect(formatCurrency(1200)).toBe('$1,200.00');
  });

  it('shows Arizona wall-clock time (UTC-7, no daylight saving)', () => {
    expect(formatDateTime('2030-07-01T17:30:00Z')).toBe('Mon, Jul 1, 10:30 AM');
    expect(formatDateTime('2030-01-15T17:30:00Z')).toBe('Tue, Jan 15, 10:30 AM');
  });

  it('formats calendar dates in Arizona time', () => {
    expect(formatDate('2030-03-01T03:00:00Z')).toBe('Feb 28, 2030');
  });

  it('describes weekly limits', () => {
    expect(formatWeeklyLimit(null)).toBe('Unlimited classes');
    expect(formatWeeklyLimit(3)).toBe('3 classes per week');
  });
});
