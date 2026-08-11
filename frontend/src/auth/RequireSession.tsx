import { Box, CircularProgress } from '@mui/material';
import { Navigate, Outlet, useLocation } from 'react-router-dom';

import { useSession } from './useSession';

/** Barra as rotas privadas: sem sessão, manda para o login. */
export function RequireSession() {
  const { user, isLoading } = useSession();
  const location = useLocation();

  // Sem esperar o `me`, um recarregamento pisca a tela de login antes do conteúdo.
  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 10 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!user) {
    // Guarda o destino para o login devolver o usuário a ele.
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  return <Outlet />;
}
