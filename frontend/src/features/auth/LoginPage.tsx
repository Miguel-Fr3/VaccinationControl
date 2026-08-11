import { useState } from 'react';
import { Alert, Button, Container, Link, Paper, Stack, TextField, Typography } from '@mui/material';
import { useForm } from 'react-hook-form';
import { Link as RouterLink, Navigate, useLocation } from 'react-router-dom';

import { applyValidationErrors, errorMessage } from '../../api/problemDetails';
import type { Credentials } from '../../api/types';
import { useSession } from '../../auth/useSession';
import { useLogin } from './useAuth';

export default function LoginPage() {
  const { user } = useSession();
  const location = useLocation();
  const [alerta, setAlerta] = useState<string | null>(null);
  const login = useLogin();

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<Credentials>({ defaultValues: { email: '', password: '' } });

  // Destino guardado pelo RequireSession quando ele interceptou a navegação.
  const origem = location.state as { from?: string } | null;
  const destino = origem?.from ?? '/';

  if (user) {
    return <Navigate to={destino} replace />;
  }

  const enviar = handleSubmit(credentials => {
    setAlerta(null);

    login.mutate(credentials, {
      onError: error => {
        // 401 de credencial inválida não traz dicionário de campos: vira alerta.
        if (!applyValidationErrors(error, setError)) {
          setAlerta(errorMessage(error));
        }
      },
    });
  });

  return (
    <Container maxWidth="xs" sx={{ py: 8 }}>
      <Paper sx={{ p: 4 }}>
        <Stack component="form" spacing={3} noValidate onSubmit={event => void enviar(event)}>
          <Typography variant="h2">Entrar</Typography>

          {alerta && <Alert severity="error">{alerta}</Alert>}

          <TextField
            label="E-mail"
            type="email"
            autoComplete="email"
            autoFocus
            error={!!errors.email}
            helperText={errors.email?.message}
            {...register('email')}
          />

          <TextField
            label="Senha"
            type="password"
            autoComplete="current-password"
            error={!!errors.password}
            helperText={errors.password?.message}
            {...register('password')}
          />

          <Button type="submit" variant="contained" size="large" disabled={login.isPending}>
            {login.isPending ? 'Entrando...' : 'Entrar'}
          </Button>

          <Typography variant="body2" align="center">
            Ainda não tem conta?{' '}
            <Link component={RouterLink} to="/registrar">
              Cadastre-se
            </Link>
          </Typography>
        </Stack>
      </Paper>
    </Container>
  );
}
