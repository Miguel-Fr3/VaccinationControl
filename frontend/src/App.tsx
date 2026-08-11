import { Navigate, Route, Routes } from 'react-router-dom';

import { RequireSession } from './auth/RequireSession';
import { AppLayout } from './components/AppLayout';
import LoginPage from './features/auth/LoginPage';
import RegisterPage from './features/auth/RegisterPage';
import PeoplePage from './features/people/PeoplePage';
import VaccinesPage from './features/vaccines/VaccinesPage';

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/registrar" element={<RegisterPage />} />

      <Route element={<RequireSession />}>
        <Route element={<AppLayout />}>
          <Route path="/" element={<Navigate to="/vacinas" replace />} />
          <Route path="/vacinas" element={<VaccinesPage />} />
          <Route path="/pessoas" element={<PeoplePage />} />
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
