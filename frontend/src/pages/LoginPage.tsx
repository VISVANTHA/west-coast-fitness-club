import { useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { api, messageOf } from '../api/client';
import { useAuth } from '../auth/useAuth';
import { firstError, validateEmail, validateRequired } from '../validation/validators';

export function LoginPage() {
  const { signIn } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const invalid = firstError(validateEmail(email), validateRequired('Password', password));
    if (invalid !== null) {
      setError(invalid);
      return;
    }

    setBusy(true);
    setError(null);
    try {
      signIn(await api.login(email.trim(), password));
      void navigate('/classes');
    } catch (failure) {
      setError(messageOf(failure));
    } finally {
      setBusy(false);
    }
  };

  return (
    <section>
      <h1>Sign in</h1>
      <form onSubmit={(event) => void submit(event)} noValidate>
        <label>
          Email
          <input type="email" autoComplete="username" value={email} onChange={(e) => setEmail(e.target.value)} />
        </label>
        <label>
          Password
          <input
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </label>
        {error !== null && <p role="alert">{error}</p>}
        <button type="submit" disabled={busy}>
          {busy ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
    </section>
  );
}
