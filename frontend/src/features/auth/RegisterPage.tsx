import { useState } from 'react';
import { Alert, Button, Container, Link, Paper, Stack, TextField, Typography } from '@mui/material';
import { useForm } from 'react-hook-form';
import { Link as RouterLink, Navigate } from 'react-router-dom';

import { applyValidationErrors, errorMessage } from '../../api/problemDetails';
import type { Credentials } from '../../api/types';
import { useSession } from '../../auth/useSession';
import { useRegister } from './useAuth';

export default function RegisterPage() {
  const { user } = useSession();
  const [alerta, setAlerta] = useState<string | null>(null);
  const cadastro = useRegister();

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<Credentials>({ defaultValues: { email: '', password: '' } });

  // O cadastro já abre a sessão, então quem tem sessão não tem o que fazer aqui.
  if (user) {
    return <Navigate to="/" replace />;
  }

  const enviar = handleSubmit(credentials => {
    setAlerta(null);

    cadastro.mutate(credentials, {
      onError: error => {
        // 409 de e-mail já cadastrado não traz dicionário de campos: vira alerta.
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
          <Typography variant="h2">Criar conta</Typography>

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
            autoComplete="new-password"
            error={!!errors.password}
            helperText={errors.password?.message ?? 'No mínimo 8 caracteres.'}
            {...register('password')}
          />

          <Button type="submit" variant="contained" size="large" disabled={cadastro.isPending}>
            {cadastro.isPending ? 'Cadastrando...' : 'Cadastrar'}
          </Button>

          <Typography variant="body2" align="center">
            Já tem conta?{' '}
            <Link component={RouterLink} to="/login">
              Entrar
            </Link>
          </Typography>
        </Stack>
      </Paper>
    </Container>
  );
}
