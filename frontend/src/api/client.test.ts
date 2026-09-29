import { describe, expect, it } from 'vitest';
import { api, ApiError, messageOf } from './client';
import { MEMBER_ID, memberSession, mockFetch } from '../test/helpers';
import { saveSession } from '../auth/session';

describe('api client', () => {
  it('calls the API under /api and sends JSON bodies', async () => {
    const calls = mockFetch(() => ({ body: { token: 't' } }));

    await api.login('ana@example.com', 'secret-passphrase');

    expect(calls[0]?.key).toBe('POST /auth/login');
    expect(calls[0]?.body).toEqual({ email: 'ana@example.com', password: 'secret-passphrase' });
    expect(calls[0]?.headers['Content-Type']).toBe('application/json');
  });

  it('attaches the bearer token only when signed in', async () => {
    const calls = mockFetch(() => ({ body: [] }));

    await api.plans();
    saveSession(memberSession);
    await api.plans();

    expect(calls[0]?.headers.Authorization).toBeUndefined();
    expect(calls[1]?.headers.Authorization).toBe('Bearer member-token');
  });

  it('maps each call to its endpoint', async () => {
    const calls = mockFetch((method) => (method === 'DELETE' ? { status: 204 } : { body: {} }));

    await api.register({ email: 'a@b.co', password: 'p'.repeat(12), firstName: 'A', lastName: 'B', phone: '1' });
    await api.classes();
    await api.summary(MEMBER_ID);
    await api.changePlan(MEMBER_ID, 'plan-2', 'tok');
    await api.bookings(MEMBER_ID);
    await api.book(MEMBER_ID, 'class-1');
    await api.cancelBooking('booking-1');
    await api.payments(MEMBER_ID);
    await api.charge(MEMBER_ID, 10, 'tok', 'Guest pass');

    expect(calls.map((c) => c.key)).toEqual([
      'POST /auth/register',
      'GET /classes',
      `GET /members/${MEMBER_ID}/summary`,
      `PUT /members/${MEMBER_ID}/plan`,
      `GET /members/${MEMBER_ID}/bookings`,
      `POST /members/${MEMBER_ID}/bookings`,
      'DELETE /bookings/booking-1',
      `GET /members/${MEMBER_ID}/payments`,
      `POST /members/${MEMBER_ID}/payments`,
    ]);
  });

  it('returns undefined for 204 responses', async () => {
    mockFetch(() => ({ status: 204 }));

    await expect(api.cancelBooking('b1')).resolves.toBeUndefined();
  });

  it('throws ApiError with the problem detail', async () => {
    mockFetch(() => ({ status: 409, body: { detail: 'This class is full.' } }));

    const failure = await api.book(MEMBER_ID, 'c1').catch((error: unknown) => error);

    expect(failure).toBeInstanceOf(ApiError);
    expect((failure as ApiError).status).toBe(409);
    expect((failure as ApiError).message).toBe('This class is full.');
  });

  it('falls back to a generic message when the error body is unusable', async () => {
    mockFetch(() => ({ status: 500, body: {} }));

    await expect(api.plans()).rejects.toThrow('Request failed (500).');
  });

  it('describes unknown errors safely', () => {
    expect(messageOf(new Error('boom'))).toBe('boom');
    expect(messageOf('nope')).toBe('Something went wrong.');
  });
});
