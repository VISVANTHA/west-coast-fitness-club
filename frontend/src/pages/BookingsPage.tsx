import { useCallback, useState } from 'react';
import { api, messageOf } from '../api/client';
import { LoadView } from '../components/LoadView';
import { RequireMember } from '../components/RequireMember';
import { formatDateTime } from '../format/format';
import { useLoad } from '../hooks/useLoad';

function MemberBookings({ memberId }: { memberId: string }) {
  const loader = useCallback(() => api.bookings(memberId), [memberId]);
  const { state, reload } = useLoad(loader);
  const [message, setMessage] = useState<string | null>(null);

  const cancel = async (bookingId: string) => {
    try {
      await api.cancelBooking(bookingId);
      setMessage('Booking cancelled.');
      reload();
    } catch (failure) {
      setMessage(messageOf(failure));
    }
  };

  return (
    <section>
      <h1>My bookings</h1>
      {message !== null && <p role="status">{message}</p>}
      <LoadView state={state}>
        {(bookings) =>
          bookings.length === 0 ? (
            <p>You have no bookings yet.</p>
          ) : (
            <ul className="cards">
              {bookings.map((booking) => (
                <li key={booking.id}>
                  <h2>{booking.title}</h2>
                  <p>
                    {booking.location} · {formatDateTime(booking.startsAt)}
                  </p>
                  <p>{booking.status}</p>
                  {booking.status === 'Confirmed' && (
                    <button type="button" onClick={() => void cancel(booking.id)}>
                      Cancel
                    </button>
                  )}
                </li>
              ))}
            </ul>
          )
        }
      </LoadView>
    </section>
  );
}

export function BookingsPage() {
  return <RequireMember>{(memberId) => <MemberBookings memberId={memberId} />}</RequireMember>;
}
