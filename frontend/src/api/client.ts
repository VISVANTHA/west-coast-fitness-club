import { getToken } from '../auth/session';
import type {
  AuthResult,
  BookingView,
  ClassSummary,
  MembershipPlan,
  MembershipSummary,
  PaymentView,
  PlanChangeResult,
  RegisterInput,
} from './types';

const BASE_URL = '/api';

export class ApiError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  body?: unknown;
}

async function readProblem(response: Response): Promise<string> {
  try {
    const problem = (await response.json()) as { detail?: string };
    return problem.detail ?? `Request failed (${response.status}).`;
  } catch {
    return `Request failed (${response.status}).`;
  }
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  const token = getToken();
  if (token !== null) {
    headers.Authorization = `Bearer ${token}`;
  }
  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  const response = await fetch(`${BASE_URL}${path}`, {
    method: options.method ?? 'GET',
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  });

  if (!response.ok) {
    throw new ApiError(response.status, await readProblem(response));
  }
  return response.status === 204 ? (undefined as T) : ((await response.json()) as T);
}

export const api = {
  login: (email: string, password: string) =>
    request<AuthResult>('/auth/login', { method: 'POST', body: { email, password } }),
  register: (input: RegisterInput) => request<AuthResult>('/auth/register', { method: 'POST', body: input }),
  plans: () => request<MembershipPlan[]>('/plans'),
  classes: () => request<ClassSummary[]>('/classes'),
  summary: (memberId: string) => request<MembershipSummary>(`/members/${memberId}/summary`),
  changePlan: (memberId: string, newPlanId: string, paymentToken: string) =>
    request<PlanChangeResult>(`/members/${memberId}/plan`, { method: 'PUT', body: { newPlanId, paymentToken } }),
  bookings: (memberId: string) => request<BookingView[]>(`/members/${memberId}/bookings`),
  book: (memberId: string, classId: string) =>
    request<BookingView>(`/members/${memberId}/bookings`, { method: 'POST', body: { classId } }),
  cancelBooking: (bookingId: string) => request<undefined>(`/bookings/${bookingId}`, { method: 'DELETE' }),
  payments: (memberId: string) => request<PaymentView[]>(`/members/${memberId}/payments`),
  charge: (memberId: string, amount: number, paymentToken: string, description: string) =>
    request<PaymentView>(`/members/${memberId}/payments`, {
      method: 'POST',
      body: { amount, paymentToken, description },
    }),
};

export function messageOf(error: unknown): string {
  return error instanceof Error ? error.message : 'Something went wrong.';
}
