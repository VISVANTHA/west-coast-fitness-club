import { useCallback, useEffect, useState } from 'react';
import { messageOf } from '../api/client';

export type LoadState<T> =
  | { status: 'loading' }
  | { status: 'ready'; data: T }
  | { status: 'error'; message: string };

/** Runs an async loader on mount (and on reload). The loader must be referentially stable. */
export function useLoad<T>(loader: () => Promise<T>): { state: LoadState<T>; reload: () => void } {
  const [state, setState] = useState<LoadState<T>>({ status: 'loading' });
  const [version, setVersion] = useState(0);

  useEffect(() => {
    let active = true;
    loader().then(
      (data) => {
        if (active) {
          setState({ status: 'ready', data });
        }
      },
      (error: unknown) => {
        if (active) {
          setState({ status: 'error', message: messageOf(error) });
        }
      },
    );
    return () => {
      active = false;
    };
  }, [loader, version]);

  const reload = useCallback(() => {
    setState({ status: 'loading' });
    setVersion((current) => current + 1);
  }, []);

  return { state, reload };
}
