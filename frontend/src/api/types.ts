export type UserRole = 'Member' | 'Administrator';
export type BookingStatus = 'Confirmed' | 'Cancelled';
export type PaymentStatus = 'Approved' | 'Declined';

export interface AuthResult {
  token: string;
  expiresAt: string;
  userId: string;
  memberId: string | null;
  role: UserRole;
}

export interface RegisterInput {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phone: string;
}

export interface MembershipPlan {
  id: string;
  name: string;
  monthlyPrice: number;
  maxClassesPerWeek: number | null;
}

export interface ClassSummary {
  id: string;
  title: string;
  trainerName: string;
  location: string;
  startsAt: string;
  durationMinutes: number;
  capacity: number;
  spotsLeft: number;
  announcement: string;
}

export interface BookingView {
  id: string;
  fitnessClassId: string;
  title: string;
  location: string;
  startsAt: string;
  status: BookingStatus;
}

export interface PaymentView {
  id: string;
  amount: number;
  currency: string;
  description: string;
  status: PaymentStatus;
  reference: string;
  createdAt: string;
}

export interface MembershipSummary {
  memberId: string;
  fullName: string;
  planName: string;
  monthlyPrice: number;
  joinedOn: string;
}

export interface PlanChangeResult {
  membershipPlanId: string;
  amountCharged: number;
}
