import { useCallback, useState } from 'react';
import type { FormEvent } from 'react';
import { api, messageOf } from '../api/client';
import { LoadView } from '../components/LoadView';
import { RequireMember } from '../components/RequireMember';
import { formatCurrency, formatDateTime } from '../format/format';
import { useLoad } from '../hooks/useLoad';
import { validateAmount } from '../validation/validators';

const DEMO_PAYMENT_TOKEN = 'tok_demo_visa';

function MemberPayments({ memberId }: { memberId: string }) {
  const loader = useCallback(() => api.payments(memberId), [memberId]);
  const { state, reload } = useLoad(loader);
  const [amount, setAmount] = useState('');
  const [message, setMessage] = useState<string | null>(null);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const invalid = validateAmount(amount);
    if (invalid !== null) {
      setMessage(invalid);
      return;
    }
    try {
      await api.charge(memberId, Number(amount), DEMO_PAYMENT_TOKEN, 'Guest pass');
      setMessage('Payment approved.');
      setAmount('');
      reload();
    } catch (failure) {
      setMessage(messageOf(failure));
    }
  };

  return (
    <section>
      <h1>Payments</h1>
      <form onSubmit={(event) => void submit(event)} noValidate>
        <label>
          Guest pass amount (USD)
          <input inputMode="decimal" value={amount} onChange={(e) => setAmount(e.target.value)} />
        </label>
        <button type="submit">Pay</button>
      </form>
      {message !== null && <p role="status">{message}</p>}
      <LoadView state={state}>
        {(payments) => (
          <table>
            <thead>
              <tr>
                <th>Date</th>
                <th>Description</th>
                <th>Amount</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {payments.map((payment) => (
                <tr key={payment.id}>
                  <td>{formatDateTime(payment.createdAt)}</td>
                  <td>{payment.description}</td>
                  <td>{formatCurrency(payment.amount)}</td>
                  <td>{payment.status}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </LoadView>
    </section>
  );
}

export function PaymentsPage() {
  return <RequireMember>{(memberId) => <MemberPayments memberId={memberId} />}</RequireMember>;
}
