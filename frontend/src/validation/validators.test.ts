import { describe, expect, it } from 'vitest';
import { bookingBlock, classBadge, describeSpots } from './booking';
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

  it.each([
    ['2030-01-01T00:20:00Z', 5, 'Starting soon'],
    ['2030-01-01T05:00:00Z', 5, 'Today'],
    ['2030-01-02T06:00:00Z', 5, 'Tomorrow'],
    ['2030-01-03T12:00:00Z', 5, 'This week'],
    ['2030-02-01T00:00:00Z', 5, 'Later'],
    ['2030-01-01T05:00:00Z', 1, 'Last spot today'],
    ['2030-01-01T05:00:00Z', 2, 'Almost full today'],
    ['2030-01-01T05:00:00Z', 0, 'Sold out'],
    ['2029-12-31T23:30:00Z', 5, 'Just started'],
    ['2029-12-31T10:00:00Z', 5, 'Earlier today'],
    ['2029-12-01T10:00:00Z', 5, 'Past'],
  ])('badges a class starting %s with %i spots as %s', (startsAt, spotsLeft, expected) => {
    expect(classBadge({ ...base, startsAt, spotsLeft }, now)).toBe(expected);
  });

  it('describes remaining spots', () => {
    expect(describeSpots(0)).toBe('Full');
    expect(describeSpots(1)).toBe('1 spot left');
    expect(describeSpots(7)).toBe('7 spots left');
  });
});
