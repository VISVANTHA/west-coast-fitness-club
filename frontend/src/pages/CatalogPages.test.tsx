import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { ClassesPage } from './ClassesPage';
import { PlansPage } from './PlansPage';
import type { ClassSummary, MembershipPlan } from '../api/types';
import { MEMBER_ID, memberSession, mockFetch, renderApp } from '../test/helpers';

const plans: MembershipPlan[] = [
  { id: 'p1', name: 'Basic', monthlyPrice: 29.99, maxClassesPerWeek: 3 },
  { id: 'p2', name: 'Premium', monthlyPrice: 79.99, maxClassesPerWeek: null },
];

const classes: ClassSummary[] = [
  {
    id: 'c1',
    title: 'Desert Spin',
    trainerName: 'Dana Whitfield',
    location: 'Phoenix',
    startsAt: '2099-01-01T17:00:00Z',
    durationMinutes: 45,
    capacity: 20,
    spotsLeft: 5,
    announcement: '',
  },
  {
    id: 'c2',
    title: 'Saguaro Yoga',
    trainerName: 'Priya Nair',
    location: 'Tucson',
    startsAt: '2099-01-02T17:00:00Z',
    durationMinutes: 60,
    capacity: 10,
    spotsLeft: 0,
    announcement: '',
  },
];

describe('PlansPage', () => {
  it('lists plans for anonymous visitors without purchase buttons', async () => {
    mockFetch(() => ({ body: plans }));
    renderApp(<PlansPage />);

    expect(await screen.findByText('Basic')).toBeInTheDocument();
    expect(screen.getByText('$79.99 / month')).toBeInTheDocument();
    expect(screen.getByText('Unlimited classes')).toBeInTheDocument();
    expect(screen.queryByRole('button')).toBeNull();
  });

  it('shows the current plan and lets a member switch', async () => {
    let planId = 'p1';
    const calls = mockFetch((method, path) => {
      if (path === '/plans') return { body: plans };
      if (method === 'PUT') {
        planId = 'p2';
        return { body: { membershipPlanId: 'p2', amountCharged: 24.19 } };
      }
      return {
        body: { memberId: MEMBER_ID, fullName: 'Ana Reyes', planName: planId === 'p1' ? 'Basic' : 'Premium', monthlyPrice: 29.99, joinedOn: '2030-01-01T00:00:00Z' },
      };
    });
    renderApp(<PlansPage />, { session: memberSession });

    expect(await screen.findByText('Your current plan: Basic')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Switch to Basic' })).toBeNull();
    await userEvent.click(screen.getByRole('button', { name: 'Switch to Premium' }));

    expect(await screen.findByText('Switched to Premium. Charged $24.19.')).toBeInTheDocument();
    expect(await screen.findByText('Your current plan: Premium')).toBeInTheDocument();
    expect(calls.some((c) => c.key === `PUT /members/${MEMBER_ID}/plan`)).toBe(true);
  });

  it('reports a declined plan change', async () => {
    mockFetch((method, path) => {
      if (path === '/plans') return { body: plans };
      if (method === 'PUT') return { status: 402, body: { detail: 'The payment for the plan change was declined.' } };
      return { body: { memberId: MEMBER_ID, fullName: 'Ana', planName: 'Basic', monthlyPrice: 29.99, joinedOn: '2030-01-01T00:00:00Z' } };
    });
    renderApp(<PlansPage />, { session: memberSession });

    await userEvent.click(await screen.findByRole('button', { name: 'Switch to Premium' }));

    expect(await screen.findByRole('status')).toHaveTextContent('declined');
  });

  it('shows an error when plans cannot load', async () => {
    mockFetch(() => ({ status: 500, body: { detail: 'An unexpected error occurred.' } }));
    renderApp(<PlansPage />);

    expect(await screen.findByRole('alert')).toHaveTextContent('unexpected error');
  });
});

describe('ClassesPage', () => {
  it('lists classes and hides booking for anonymous visitors', async () => {
    mockFetch(() => ({ body: classes }));
    renderApp(<ClassesPage />);

    expect(await screen.findByText('Desert Spin')).toBeInTheDocument();
    expect(screen.getByText('Full')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Book' })).toBeNull();
  });

  it('books an open class and disables full ones', async () => {
    const calls = mockFetch((method) => (method === 'POST' ? { status: 201, body: {} } : { body: classes }));
    renderApp(<ClassesPage />, { session: memberSession });

    const [open, full] = (await screen.findAllByRole('button', { name: 'Book' })) as [HTMLElement, HTMLElement];
    expect(full).toBeDisabled();
    await userEvent.click(open);

    expect(await screen.findByText('You are booked into Desert Spin.')).toBeInTheDocument();
    expect(calls.some((c) => c.key === `POST /members/${MEMBER_ID}/bookings`)).toBe(true);
  });

  it('shows why a booking failed', async () => {
    mockFetch((method) =>
      method === 'POST' ? { status: 409, body: { detail: 'You are already booked into this class.' } } : { body: classes },
    );
    renderApp(<ClassesPage />, { session: memberSession });

    const [open] = (await screen.findAllByRole('button', { name: 'Book' })) as [HTMLElement];
    await userEvent.click(open);

    expect(await screen.findByRole('status')).toHaveTextContent('already booked');
  });

  it('does nothing for an administrator without a member profile', async () => {
    const calls = mockFetch(() => ({ body: classes }));
    renderApp(<ClassesPage />, { session: { ...memberSession, memberId: null, role: 'Administrator' } });

    await screen.findByText('Desert Spin');

    expect(screen.queryByRole('button', { name: 'Book' })).toBeNull();
    expect(calls).toHaveLength(1);
  });
});
