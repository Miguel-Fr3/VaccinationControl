import { useState } from 'react';
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
} from '@mui/material';
import { useForm } from 'react-hook-form';

import { applyValidationErrors, errorMessage } from '../../api/problemDetails';
import type { CreateVaccineRequest } from '../../api/types';
import { useCreateVaccine } from './useVaccines';

type VaccineDialogProps = {
  open: boolean;
  onClose: () => void;
};

// Cadastro de vacina. O nome é o único campo do contrato.
export function VaccineDialog({ open, onClose }: VaccineDialogProps) {
  const [alertMessage, setAlertMessage] = useState<string | null>(null);
  const createVaccine = useCreateVaccine();

  const {
    register,
    handleSubmit,
    setError,
    reset,
    formState: { errors },
  } = useForm<CreateVaccineRequest>({ defaultValues: { name: '' } });

  const close = () => {
    reset();
    setAlertMessage(null);
    onClose();
  };

  const submit = handleSubmit(body => {
    setAlertMessage(null);

    createVaccine.mutate(body, {
      onSuccess: close,
      onError: error => {
        if (!applyValidationErrors(error, setError)) {
          setAlertMessage(errorMessage(error));
        }
      },
    });
  });

  return (
    <Dialog open={open} onClose={close} fullWidth maxWidth="xs">
      <form noValidate onSubmit={event => void submit(event)}>
        <DialogTitle>Nova vacina</DialogTitle>

        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {alertMessage && <Alert severity="error">{alertMessage}</Alert>}

            <TextField
              label="Nome"
              autoFocus
              fullWidth
              error={!!errors.name}
              helperText={errors.name?.message}
              {...register('name')}
            />
          </Stack>
        </DialogContent>

        <DialogActions>
          <Button onClick={close} disabled={createVaccine.isPending}>
            Cancelar
          </Button>
          <Button type="submit" variant="contained" disabled={createVaccine.isPending}>
            {createVaccine.isPending ? 'Salvando...' : 'Salvar'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}
