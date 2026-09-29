import { useState } from 'react';
import { api, messageOf } from '../api/client';
import { useAuth } from '../auth/useAuth';
import { LoadView } from '../components/LoadView';
import { formatDateTime } from '../format/format';
import { useLoad } from '../hooks/useLoad';
import { bookingBlock, describeSpots } from '../validation/booking';

export function ClassesPage() {
  const { session } = useAuth();
  const memberId = session?.memberId ?? null;
  const { state, reload } = useLoad(api.classes);
  const [message, setMessage] = useState<string | null>(null);

  const book = async (classId: string, title: string) => {
    if (memberId === null) {
      return;
    }
    try {
      await api.book(memberId, classId);
      setMessage(`You are booked into ${title}.`);
      reload();
    } catch (failure) {
      setMessage(messageOf(failure));
    }
  };

  return (
    <section>
      <h1>Upcoming classes</h1>
      {message !== null && <p role="status">{message}</p>}
      <LoadView state={state}>
        {(classes) => (
          <ul className="cards">
            {classes.map((item) => (
              <li key={item.id}>
                <h2>{item.title}</h2>
                <p>
                  {item.location} · {item.trainerName}
                </p>
                <p>{formatDateTime(item.startsAt)} (Arizona time)</p>
                <p>{describeSpots(item.spotsLeft)}</p>
                {memberId !== null && (
                  <button
                    type="button"
                    disabled={bookingBlock(item) !== null}
                    onClick={() => void book(item.id, item.title)}
                  >
                    Book
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
