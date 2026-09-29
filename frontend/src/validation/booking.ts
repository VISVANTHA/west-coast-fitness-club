import type { ClassSummary } from '../api/types';

export type BookingBlock = 'started' | 'full' | null;

/** Client-side hint only; the API re-checks every booking rule. */
export function bookingBlock(fitnessClass: ClassSummary, now: Date = new Date()): BookingBlock {
  if (new Date(fitnessClass.startsAt) <= now) {
    return 'started';
  }
  return fitnessClass.spotsLeft <= 0 ? 'full' : null;
}

export function describeSpots(spotsLeft: number): string {
  if (spotsLeft <= 0) {
    return 'Full';
  }
  return spotsLeft === 1 ? '1 spot left' : `${spotsLeft} spots left`;
}
