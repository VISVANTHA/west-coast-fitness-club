/** Arizona does not observe daylight saving time, so all clubs share one wall clock. */
export const CLUB_TIME_ZONE = 'America/Phoenix';

const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' });

const dateTime = new Intl.DateTimeFormat('en-US', {
  timeZone: CLUB_TIME_ZONE,
  weekday: 'short',
  month: 'short',
  day: 'numeric',
  hour: 'numeric',
  minute: '2-digit',
});

const date = new Intl.DateTimeFormat('en-US', {
  timeZone: CLUB_TIME_ZONE,
  year: 'numeric',
  month: 'short',
  day: 'numeric',
});

export function formatCurrency(amount: number): string {
  return currency.format(amount);
}

export function formatDateTime(iso: string): string {
  return dateTime.format(new Date(iso));
}

export function formatDate(iso: string): string {
  return date.format(new Date(iso));
}

export function formatWeeklyLimit(limit: number | null): string {
  return limit === null ? 'Unlimited classes' : `${limit} classes per week`;
}
