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
import { Controller, useForm } from 'react-hook-form';

import { applyValidationErrors, errorMessage } from '../../api/problemDetails';
import type { CreatePersonRequest } from '../../api/types';
import { formatCpf, stripCpf } from '../../format/cpf';
import { useCreatePerson } from './usePeople';

type PersonDialogProps = {
  open: boolean;
  onClose: () => void;
};

// Cadastro de pessoa: nome e documento.
export function PersonDialog({ open, onClose }: PersonDialogProps) {
  const [alertMessage, setAlertMessage] = useState<string | null>(null);
  const createPerson = useCreatePerson();

  const {
    register,
    control,
    handleSubmit,
    setError,
    reset,
    formState: { errors },
  } = useForm<CreatePersonRequest>({ defaultValues: { name: '', document: '' } });

  const close = () => {
    reset();
    setAlertMessage(null);
    onClose();
  };

  const submit = handleSubmit(body => {
    setAlertMessage(null);

    createPerson.mutate(body, {
      onSuccess: close,
      onError: error => {
        // 409 de documento repetido não traz dicionário de campos: vira alerta.
        if (!applyValidationErrors(error, setError)) {
          setAlertMessage(errorMessage(error));
        }
      },
    });
  });

  return (
    <Dialog open={open} onClose={close} fullWidth maxWidth="xs">
      <form noValidate onSubmit={event => void submit(event)}>
        <DialogTitle>Nova pessoa</DialogTitle>

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

            {/* O campo guarda só os dígitos e mascara na exibição: assim o corpo enviado
                já sai no formato da API, sem limpeza na hora do submit. */}
            <Controller
              name="document"
              control={control}
              render={({ field }) => (
                <TextField
                  label="CPF"
                  fullWidth
                  value={formatCpf(field.value)}
                  onChange={event => {
                    field.onChange(stripCpf(event.target.value));
                  }}
                  onBlur={field.onBlur}
                  inputRef={field.ref}
                  slotProps={{ htmlInput: { inputMode: 'numeric', maxLength: 14 } }}
                  error={!!errors.document}
                  helperText={errors.document?.message}
                />
              )}
            />
          </Stack>
        </DialogContent>

        <DialogActions>
          <Button onClick={close} disabled={createPerson.isPending}>
            Cancelar
          </Button>
          <Button type="submit" variant="contained" disabled={createPerson.isPending}>
            {createPerson.isPending ? 'Salvando...' : 'Salvar'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}
