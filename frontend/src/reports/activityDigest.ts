import groupBy from 'lodash/groupBy';
import type { BookingView } from '../api/types';

export interface ActivityDigest {
  total: number;
  confirmed: number;
  cancelled: number;
  upcoming: number;
  cancellationRate: number;
  favoriteLocation: string | null;
  busiestWeekday: number | null;
  longestStreakDays: number;
  headline: string;
}

// INTENTIONAL ISSUE (duplicated block): activityReport.ts is the original of this file; only the exported names
// differ, so every helper below exists twice (copy). Both files are untested (the low-coverage module).
// See docs/clarifications.md, "Seeded findings".

const DAY_MS = 24 * 60 * 60 * 1000;

function countByStatus(bookings: BookingView[], status: BookingView['status']): number {
  let count = 0;
  for (const booking of bookings) {
    if (booking.status === status) {
      count += 1;
    }
  }
  return count;
}

function countUpcoming(bookings: BookingView[], now: Date): number {
  let count = 0;
  for (const booking of bookings) {
    if (booking.status === 'Confirmed' && new Date(booking.startsAt) > now) {
      count += 1;
    }
  }
  return count;
}

function cancellationRate(confirmed: number, cancelled: number): number {
  const total = confirmed + cancelled;
  if (total === 0) {
    return 0;
  }
  return Math.round((cancelled / total) * 1000) / 10;
}

function favoriteLocation(bookings: BookingView[]): string | null {
  const groups = groupBy(bookings, (booking) => booking.location.trim().toLowerCase());
  const entries = Object.entries(groups).filter(([key]) => key !== '');
  entries.sort(([keyA, a], [keyB, b]) => b.length - a.length || (keyA < keyB ? -1 : 1));
  return entries[0]?.[1][0]?.location ?? null;
}

function busiestWeekday(bookings: BookingView[]): number | null {
  const counts = [0, 0, 0, 0, 0, 0, 0];
  let any = false;
  for (const booking of bookings) {
    const day = new Date(booking.startsAt).getUTCDay();
    counts[day] = (counts[day] ?? 0) + 1;
    any = true;
  }
  if (!any) {
    return null;
  }
  let bestDay = 0;
  for (let day = 1; day < counts.length; day += 1) {
    if ((counts[day] ?? 0) > (counts[bestDay] ?? 0)) {
      bestDay = day;
    }
  }
  return bestDay;
}

function startOfUtcDay(iso: string): number {
  const date = new Date(iso);
  return Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate());
}

function longestStreak(bookings: BookingView[]): number {
  const days = new Set<number>();
  for (const booking of bookings) {
    if (booking.status === 'Confirmed') {
      days.add(startOfUtcDay(booking.startsAt));
    }
  }
  const sorted = [...days].sort((a, b) => a - b);
  let longest = 0;
  let current = 0;
  let previous: number | null = null;
  for (const day of sorted) {
    current = previous !== null && day - previous === DAY_MS ? current + 1 : 1;
    longest = Math.max(longest, current);
    previous = day;
  }
  return longest;
}

function buildHeadline(confirmed: number, cancelled: number, upcoming: number, favorite: string | null): string {
  if (confirmed === 0 && cancelled === 0) {
    return 'No class activity yet.';
  }
  const parts = [`${confirmed} confirmed`, `${cancelled} cancelled`, `${upcoming} upcoming`];
  if (favorite !== null) {
    parts.push(`favourite club ${favorite}`);
  }
  return `${parts.join(', ')}.`;
}

function describeTrend(confirmed: number, cancelled: number): string {
  if (confirmed > cancelled * 3) {
    return 'Consistent attendance.';
  }
  if (confirmed > cancelled) {
    return 'Mostly attending.';
  }
  return confirmed === cancelled ? 'Attendance is split evenly.' : 'Frequently cancelling.';
}

function medianLeadDays(bookings: BookingView[], now: Date): number {
  const leads = bookings.map((booking) => (new Date(booking.startsAt).getTime() - now.getTime()) / DAY_MS);
  if (leads.length === 0) {
    return 0;
  }
  leads.sort((a, b) => a - b);
  const middle = Math.floor(leads.length / 2);
  const median = leads.length % 2 === 1 ? (leads[middle] ?? 0) : ((leads[middle - 1] ?? 0) + (leads[middle] ?? 0)) / 2;
  return Math.round(median * 10) / 10;
}

function distinctLocations(bookings: BookingView[]): number {
  const seen = new Set<string>();
  for (const booking of bookings) {
    const location = booking.location.trim().toLowerCase();
    if (location !== '') {
      seen.add(location);
    }
  }
  return seen.size;
}

export function digestActivity(bookings: BookingView[], now: Date = new Date()): ActivityDigest {
  const confirmed = countByStatus(bookings, 'Confirmed');
  const cancelled = countByStatus(bookings, 'Cancelled');
  const upcoming = countUpcoming(bookings, now);
  const favorite = favoriteLocation(bookings);
  return {
    total: bookings.length,
    confirmed,
    cancelled,
    upcoming,
    cancellationRate: cancellationRate(confirmed, cancelled),
    favoriteLocation: favorite,
    busiestWeekday: busiestWeekday(bookings),
    longestStreakDays: longestStreak(bookings),
    headline: buildHeadline(confirmed, cancelled, upcoming, favorite),
  };
}

export function digestTrend(bookings: BookingView[]): string {
  return describeTrend(countByStatus(bookings, 'Confirmed'), countByStatus(bookings, 'Cancelled'));
}

export function digestReach(bookings: BookingView[], now: Date = new Date()): { medianLeadDays: number; locations: number } {
  return { medianLeadDays: medianLeadDays(bookings, now), locations: distinctLocations(bookings) };
}
