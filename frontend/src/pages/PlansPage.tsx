import { useCallback, useState } from 'react';
import { api, messageOf } from '../api/client';
import { useAuth } from '../auth/useAuth';
import { LoadView } from '../components/LoadView';
import { formatCurrency, formatWeeklyLimit } from '../format/format';
import { useLoad } from '../hooks/useLoad';

/** Demo payment token: the club's mock gateway never sees card numbers. */
const DEMO_PAYMENT_TOKEN = 'tok_demo_visa';

export function PlansPage() {
  const { session } = useAuth();
  const memberId = session?.memberId ?? null;
  const { state } = useLoad(api.plans);
  const loadSummary = useCallback(
    () => (memberId === null ? Promise.resolve(null) : api.summary(memberId)),
    [memberId],
  );
  const { state: summary, reload } = useLoad(loadSummary);
  const [message, setMessage] = useState<string | null>(null);

  const choose = async (planId: string, planName: string) => {
    if (memberId === null) {
      return;
    }
    try {
      const result = await api.changePlan(memberId, planId, DEMO_PAYMENT_TOKEN);
      setMessage(`Switched to ${planName}. Charged ${formatCurrency(result.amountCharged)}.`);
      reload();
    } catch (failure) {
      setMessage(messageOf(failure));
    }
  };

  const currentPlan = summary.status === 'ready' ? summary.data?.planName : undefined;

  return (
    <section>
      <h1>Membership plans</h1>
      {currentPlan !== undefined && <p>Your current plan: {currentPlan}</p>}
      {message !== null && <p role="status">{message}</p>}
      <LoadView state={state}>
        {(plans) => (
          <ul className="cards">
            {plans.map((plan) => (
              <li key={plan.id}>
                <h2>{plan.name}</h2>
                <p>{formatCurrency(plan.monthlyPrice)} / month</p>
                <p>{formatWeeklyLimit(plan.maxClassesPerWeek)}</p>
                {memberId !== null && plan.name !== currentPlan && (
                  <button type="button" onClick={() => void choose(plan.id, plan.name)}>
                    Switch to {plan.name}
                  </button>
                )}
              </li>
            ))}
          </ul>
        )}
      </LoadView>
    </section>
  );
}
