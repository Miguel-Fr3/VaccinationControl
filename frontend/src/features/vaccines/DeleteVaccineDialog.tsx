import { useState } from 'react';
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Stack,
} from '@mui/material';

import { errorMessage } from '../../api/problemDetails';
import type { Vaccine } from '../../api/types';
import { useDeleteVaccine } from './useVaccines';

type DeleteVaccineDialogProps = {
  vaccine: Vaccine;
  onClose: () => void;
};

/**
 * Confirmação da exclusão. Diferente da pessoa, aqui não há cascata: vacina com dose
 * registrada é recusada pela API com 409. Não há endpoint que conte as aplicações de uma
 * vacina, então a tela avisa da possibilidade e deixa a resposta do servidor decidir.
 */
export function DeleteVaccineDialog({ vaccine, onClose }: DeleteVaccineDialogProps) {
  const [alertMessage, setAlertMessage] = useState<string | null>(null);
  const deleteVaccine = useDeleteVaccine();

  const confirm = () => {
    setAlertMessage(null);

    deleteVaccine.mutate(vaccine.id, {
      onSuccess: onClose,
      onError: error => {
        setAlertMessage(errorMessage(error));
      },
    });
  };

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>Excluir vacina</DialogTitle>

      <DialogContent>
        <Stack spacing={2}>
          <DialogContentText>
            Excluir <strong>{vaccine.name}</strong> do catálogo? Esta ação não pode ser desfeita.
          </DialogContentText>

          <Alert severity="info">
            Vacinas com aplicações registradas não podem ser excluídas. Para remover esta, apague
            antes as aplicações dela nos cartões de vacinação.
          </Alert>

          {alertMessage && <Alert severity="error">{alertMessage}</Alert>}
        </Stack>
      </DialogContent>

      <DialogActions>
        <Button onClick={onClose} disabled={deleteVaccine.isPending}>
          Cancelar
        </Button>
        <Button
          color="error"
          variant="contained"
          disabled={deleteVaccine.isPending}
          onClick={confirm}
        >
          {deleteVaccine.isPending ? 'Excluindo...' : 'Excluir'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
