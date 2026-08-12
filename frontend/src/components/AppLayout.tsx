import { AppBar, Button, Container, Stack, Toolbar, Typography } from '@mui/material';
import { NavLink, Outlet } from 'react-router-dom';

import { useSession } from '../auth/useSession';
import { useLogout } from '../features/auth/useAuth';

/** Moldura das telas com sessão: barra superior, identificação e saída. */
export function AppLayout() {
  const { user } = useSession();
  const logout = useLogout();

  return (
    <>
      <AppBar position="static">
        <Toolbar>
          <Typography variant="h6" component="div" sx={{ mr: 4 }}>
            Cartão de Vacinação
          </Typography>

          <Stack direction="row" spacing={1} sx={{ flexGrow: 1 }}>
            <Button color="inherit" component={NavLink} to="/pessoas">
              Pessoas
            </Button>
            <Button color="inherit" component={NavLink} to="/vacinas">
              Vacinas
            </Button>
          </Stack>

          <Typography variant="body2" sx={{ mr: 2, display: { xs: 'none', sm: 'block' } }}>
            {user?.email}
          </Typography>

          <Button
            color="inherit"
            disabled={logout.isPending}
            onClick={() => {
              logout.mutate();
            }}
          >
            Sair
          </Button>
        </Toolbar>
      </AppBar>

      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Outlet />
      </Container>
    </>
  );
}
