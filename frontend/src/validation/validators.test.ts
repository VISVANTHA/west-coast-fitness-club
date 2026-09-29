import { describe, expect, it } from 'vitest';
import { bookingBlock, describeSpots } from './booking';
import { firstError, validateAmount, validateEmail, validatePassword, validateRequired } from './validators';
import type { ClassSummary } from '../api/types';

describe('validators', () => {
  it('accepts well-formed emails and rejects malformed ones', () => {
    expect(validateEmail(' ana@example.com ')).toBeNull();
    expect(validateEmail('ana@')).not.toBeNull();
    expect(validateEmail('ana example.com')).not.toBeNull();
  });

  it('requires a 12 character password', () => {
    expect(validatePassword('correct-horse-battery')).toBeNull();
    expect(validatePassword('short')).toMatch(/at least 12/);
  });

  it('flags blank required fields', () => {
    expect(validateRequired('Name', '  ')).toBe('Name is required.');
    expect(validateRequired('Name', 'Ana')).toBeNull();
  });

  it.each([
    ['25', null],
    ['19.99', null],
    ['', 'Enter an amount greater than zero.'],
    ['0', 'Enter an amount greater than zero.'],
    ['-4', 'Enter an amount greater than zero.'],
    ['abc', 'Enter an amount greater than zero.'],
    ['1.999', 'Use at most two decimal places.'],
  ])('validates the amount %j', (value, expected) => {
    expect(validateAmount(value)).toBe(expected);
  });

  it('returns the first error only', () => {
    expect(firstError(null, 'second', 'third')).toBe('second');
    expect(firstError(null, null)).toBeNull();
  });
});

describe('booking hints', () => {
  const base: ClassSummary = {
    id: 'c1',
    title: 'Spin',
    trainerName: 'Dana',
    location: 'Phoenix',
    startsAt: '2099-01-01T17:00:00Z',
    durationMinutes: 45,
    capacity: 10,
    spotsLeft: 3,
    announcement: '',
  };
  const now = new Date('2030-01-01T00:00:00Z');

  it('allows an upcoming class with spots', () => {
    expect(bookingBlock(base, now)).toBeNull();
  });

  it('blocks a class that has started or is full', () => {
    expect(bookingBlock({ ...base, startsAt: '2029-12-31T00:00:00Z' }, now)).toBe('started');
    expect(bookingBlock({ ...base, spotsLeft: 0 }, now)).toBe('full');
  });

  it('describes remaining spots', () => {
    expect(describeSpots(0)).toBe('Full');
    expect(describeSpots(1)).toBe('1 spot left');
    expect(describeSpots(7)).toBe('7 spots left');
  });
});
