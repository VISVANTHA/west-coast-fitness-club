import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Route, Routes } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { BookingsPage } from './BookingsPage';
import { PaymentsPage } from './PaymentsPage';
import type { BookingView, PaymentView } from '../api/types';
import { adminSession, MEMBER_ID, memberSession, mockFetch, renderApp } from '../test/helpers';

const bookings: BookingView[] = [
  { id: 'b1', fitnessClassId: 'c1', title: 'Desert Spin', location: 'Phoenix', startsAt: '2099-01-01T17:00:00Z', status: 'Confirmed' },
  { id: 'b2', fitnessClassId: 'c2', title: 'Old Yoga', location: 'Tucson', startsAt: '2030-01-01T17:00:00Z', status: 'Cancelled' },
];

const payments: PaymentView[] = [
  { id: 'p1', amount: 19.99, currency: 'USD', description: 'Guest pass', status: 'Approved', reference: 'mock-1', createdAt: '2030-01-02T17:00:00Z' },
];

function WithLogin({ children }: { children: React.ReactNode }) {
  return (
    <Routes>
      <Route path="/page" element={children} />
      <Route path="/login" element={<p>login page</p>} />
    </Routes>
  );
}

describe('BookingsPage', () => {
  it('redirects anonymous visitors to sign in', () => {
    mockFetch(() => ({}));
    renderApp(<WithLogin><BookingsPage /></WithLogin>, { route: '/page' });

    expect(screen.getByText('login page')).toBeInTheDocument();
  });

  it('explains that administrators have no bookings', () => {
    mockFetch(() => ({}));
    renderApp(<BookingsPage />, { session: adminSession });

    expect(screen.getByRole('alert')).toHaveTextContent('no member profile');
  });

  it('lists bookings and cancels a confirmed one', async () => {
    const calls = mockFetch((method) => (method === 'DELETE' ? { status: 204 } : { body: bookings }));
    renderApp(<BookingsPage />, { session: memberSession });

    expect(await screen.findByText('Desert Spin')).toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: 'Cancel' })).toHaveLength(1);
    await userEvent.click(screen.getByRole('button', { name: 'Cancel' }));

    expect(await screen.findByText('Booking cancelled.')).toBeInTheDocument();
    expect(calls.some((c) => c.key === 'DELETE /bookings/b1')).toBe(true);
  });

  it('shows an empty state', async () => {
    mockFetch(() => ({ body: [] }));
    renderApp(<BookingsPage />, { session: memberSession });

    expect(await screen.findByText('You have no bookings yet.')).toBeInTheDocument();
  });

  it('reports a failed cancellation', async () => {
    mockFetch((method) => (method === 'DELETE' ? { status: 403, body: { detail: 'You do not have access to this member\'s records.' } } : { body: bookings }));
    renderApp(<BookingsPage />, { session: memberSession });

    await userEvent.click(await screen.findByRole('button', { name: 'Cancel' }));

    expect(await screen.findByRole('status')).toHaveTextContent('do not have access');
  });
});

describe('PaymentsPage', () => {
  it('lists payment history', async () => {
    mockFetch(() => ({ body: payments }));
    renderApp(<PaymentsPage />, { session: memberSession });

    expect(await screen.findByText('$19.99')).toBeInTheDocument();
    expect(screen.getByText('Guest pass')).toBeInTheDocument();
    expect(screen.getByText('Approved')).toBeInTheDocument();
  });

  it('submits a valid amount using a payment token, never a card number', async () => {
    const calls = mockFetch((method) => (method === 'POST' ? { status: 201, body: payments[0] } : { body: payments }));
    renderApp(<PaymentsPage />, { session: memberSession });

    await userEvent.type(await screen.findByLabelText(/Guest pass amount/), '12.50');
    await userEvent.click(screen.getByRole('button', { name: 'Pay' }));

    expect(await screen.findByText('Payment approved.')).toBeInTheDocument();
    const charge = calls.find((c) => c.key === `POST /members/${MEMBER_ID}/payments`);
    expect(charge?.body).toEqual({ amount: 12.5, paymentToken: 'tok_demo_visa', description: 'Guest pass' });
  });

  it('rejects an invalid amount without calling the API', async () => {
    const calls = mockFetch(() => ({ body: payments }));
    renderApp(<PaymentsPage />, { session: memberSession });

    await userEvent.type(await screen.findByLabelText(/Guest pass amount/), '0');
    await userEvent.click(screen.getByRole('button', { name: 'Pay' }));

    expect(screen.getByRole('status')).toHaveTextContent('greater than zero');
    expect(calls.filter((c) => c.key.startsWith('POST'))).toHaveLength(0);
  });

  it('shows a decline from the gateway', async () => {
    mockFetch((method) => (method === 'POST' ? { status: 402, body: { detail: 'Declined.' } } : { body: payments }));
    renderApp(<PaymentsPage />, { session: memberSession });

    await userEvent.type(await screen.findByLabelText(/Guest pass amount/), '5');
    await userEvent.click(screen.getByRole('button', { name: 'Pay' }));

    expect(await screen.findByRole('status')).toHaveTextContent('Declined.');
  });
});
