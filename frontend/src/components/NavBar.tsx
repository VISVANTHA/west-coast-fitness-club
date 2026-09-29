import { NavLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/useAuth';

export function NavBar() {
  const { session, signOut } = useAuth();
  const navigate = useNavigate();

  const handleSignOut = () => {
    signOut();
    void navigate('/login');
  };

  return (
    <nav className="nav" aria-label="Main">
      <span className="brand">West Coast Fitness Club</span>
      <NavLink to="/plans">Plans</NavLink>
      <NavLink to="/classes">Classes</NavLink>
      {session === null ? (
        <>
          <NavLink to="/login">Sign in</NavLink>
          <NavLink to="/register">Join</NavLink>
        </>
      ) : (
        <>
          <NavLink to="/bookings">My bookings</NavLink>
          <NavLink to="/payments">Payments</NavLink>
          <button type="button" onClick={handleSignOut}>
            Sign out
          </button>
        </>
      )}
    </nav>
  );
}
