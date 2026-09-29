import { useState } from 'react';
import type { ChangeEvent, FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { api, messageOf } from '../api/client';
import type { RegisterInput } from '../api/types';
import { useAuth } from '../auth/useAuth';
import { firstError, validateEmail, validatePassword, validateRequired } from '../validation/validators';

const EMPTY: RegisterInput = { email: '', password: '', firstName: '', lastName: '', phone: '' };

const FIELDS: { name: keyof RegisterInput; label: string; type: string; autoComplete: string }[] = [
  { name: 'firstName', label: 'First name', type: 'text', autoComplete: 'given-name' },
  { name: 'lastName', label: 'Last name', type: 'text', autoComplete: 'family-name' },
  { name: 'email', label: 'Email', type: 'email', autoComplete: 'email' },
  { name: 'phone', label: 'Phone', type: 'tel', autoComplete: 'tel' },
  { name: 'password', label: 'Password', type: 'password', autoComplete: 'new-password' },
];

export function RegisterPage() {
  const { signIn } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState<RegisterInput>(EMPTY);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const update = (name: keyof RegisterInput) => (event: ChangeEvent<HTMLInputElement>) =>
    setForm((current) => ({ ...current, [name]: event.target.value }));

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const invalid = firstError(
      validateRequired('First name', form.firstName),
      validateRequired('Last name', form.lastName),
      validateEmail(form.email),
      validatePassword(form.password),
    );
    if (invalid !== null) {
      setError(invalid);
      return;
    }

    setBusy(true);
    setError(null);
    try {
      signIn(await api.register(form));
      void navigate('/plans');
    } catch (failure) {
      setError(messageOf(failure));
    } finally {
      setBusy(false);
    }
  };

  return (
    <section>
      <h1>Join West Coast Fitness Club</h1>
      <form onSubmit={(event) => void submit(event)} noValidate>
        {FIELDS.map((field) => (
          <label key={field.name}>
            {field.label}
            <input
              type={field.type}
              autoComplete={field.autoComplete}
              value={form[field.name]}
              onChange={update(field.name)}
            />
          </label>
        ))}
        {error !== null && <p role="alert">{error}</p>}
        <button type="submit" disabled={busy}>
          {busy ? 'Creating account…' : 'Create account'}
        </button>
      </form>
    </section>
  );
}
