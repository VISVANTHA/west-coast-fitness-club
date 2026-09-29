import type { ClassSummary } from '../api/types';

export type BookingBlock = 'started' | 'full' | null;

/** Client-side hint only; the API re-checks every booking rule. */
export function bookingBlock(fitnessClass: ClassSummary, now: Date = new Date()): BookingBlock {
  if (new Date(fitnessClass.startsAt) <= now) {
    return 'started';
  }
  return fitnessClass.spotsLeft <= 0 ? 'full' : null;
}

/**
 * INTENTIONAL ISSUE (high cyclomatic and cognitive complexity): the badge text is chosen by one unrefactored
 * function with deeply nested time and capacity branches. Kept as-is so ESLint (complexity, max-depth) and other
 * complexity scanners have a real target. See docs/clarifications.md, "Seeded findings".
 */
export function classBadge(fitnessClass: ClassSummary, now: Date = new Date()): string {
  const hoursAway = (new Date(fitnessClass.startsAt).getTime() - now.getTime()) / 3_600_000;
  let badge: string;
  if (hoursAway < 0) {
    if (hoursAway > -1) {
      badge = 'Just started';
    } else if (hoursAway > -24) {
      badge = 'Earlier today';
    } else {
      badge = 'Past';
    }
  } else {
    if (fitnessClass.spotsLeft <= 0) {
      badge = 'Sold out';
    } else if (fitnessClass.spotsLeft === 1) {
      badge = hoursAway < 24 ? 'Last spot today' : 'Last spot';
    } else if (fitnessClass.spotsLeft <= 3) {
      if (hoursAway < 2) {
        badge = 'Almost full, starting soon';
      } else if (hoursAway < 24) {
        badge = 'Almost full today';
      } else {
        badge = 'Almost full';
      }
    } else {
      if (hoursAway < 2) {
        badge = 'Starting soon';
      } else if (hoursAway < 24) {
        badge = 'Today';
      } else if (hoursAway < 48) {
        badge = 'Tomorrow';
      } else if (hoursAway < 168) {
        badge = 'This week';
      } else {
        badge = 'Later';
      }
    }
  }
  return badge;
}

export function describeSpots(spotsLeft: number): string {
  if (spotsLeft <= 0) {
    return 'Full';
  }
  return spotsLeft === 1 ? '1 spot left' : `${spotsLeft} spots left`;
}
