import type { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../auth/useAuth';

/** Renders children with the signed-in member's id; redirects anonymous visitors to sign in. */
export function RequireMember({ children }: { children: (memberId: string) => ReactNode }) {
  const { session } = useAuth();
  if (session === null) {
    return <Navigate to="/login" replace />;
  }
  if (session.memberId === null) {
    return <p role="alert">Administrator accounts have no member profile.</p>;
  }
  return <>{children(session.memberId)}</>;
}
