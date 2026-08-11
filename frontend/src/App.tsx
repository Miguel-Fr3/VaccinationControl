import { Typography } from '@mui/material';
import { Navigate, Route, Routes } from 'react-router-dom';

import { RequireSession } from './auth/RequireSession';
import { AppLayout } from './components/AppLayout';
import LoginPage from './features/auth/LoginPage';
import RegisterPage from './features/auth/RegisterPage';

function Home() {
  return <Typography variant="h1">Cartão de Vacinação</Typography>;
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/registrar" element={<RegisterPage />} />

      <Route element={<RequireSession />}>
        <Route element={<AppLayout />}>
          <Route path="/" element={<Home />} />
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
