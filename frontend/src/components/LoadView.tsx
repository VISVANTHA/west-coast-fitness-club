import type { ReactNode } from 'react';
import type { LoadState } from '../hooks/useLoad';

/** Shows a loading, error or ready view for a LoadState. */
export function LoadView<T>({ state, children }: { state: LoadState<T>; children: (data: T) => ReactNode }) {
  if (state.status === 'loading') {
    return <p aria-busy="true">Loading…</p>;
  }
  if (state.status === 'error') {
    return <p role="alert">{state.message}</p>;
  }
  return <>{children(state.data)}</>;
}
