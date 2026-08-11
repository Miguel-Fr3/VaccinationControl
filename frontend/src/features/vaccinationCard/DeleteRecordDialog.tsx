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
import type { VaccinationCardDose } from '../../api/types';
import { vaccinationTypeLabels } from '../../api/types';
import { formatIsoDate } from '../../format/date';
import { useDeleteVaccinationRecord } from './useVaccinationCard';

type DeleteRecordDialogProps = {
  personId: string;
  vaccineName: string;
  dose: VaccinationCardDose;
  onClose: () => void;
};

// Confirmação da remoção de uma aplicação. Sem cascata: some só este registro.
export function DeleteRecordDialog({
  personId,
  vaccineName,
  dose,
  onClose,
}: DeleteRecordDialogProps) {
  const [alertMessage, setAlertMessage] = useState<string | null>(null);
  const deleteRecord = useDeleteVaccinationRecord(personId);

  const confirm = () => {
    setAlertMessage(null);

    deleteRecord.mutate(dose.recordId, {
      onSuccess: onClose,
      onError: error => {
        setAlertMessage(errorMessage(error));
      },
    });
  };

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>Remover aplicação</DialogTitle>

      <DialogContent>
        <Stack spacing={2}>
          <DialogContentText>
            Remover a {vaccinationTypeLabels[dose.vaccinationType].toLowerCase()}{' '}
            <strong>{dose.doseNumber}</strong> de <strong>{vaccineName}</strong>, aplicada em{' '}
            {formatIsoDate(dose.vaccinationDate)}? Esta ação não pode ser desfeita.
          </DialogContentText>

          {alertMessage && <Alert severity="error">{alertMessage}</Alert>}
        </Stack>
      </DialogContent>

      <DialogActions>
        <Button onClick={onClose} disabled={deleteRecord.isPending}>
          Cancelar
        </Button>
        <Button
          color="error"
          variant="contained"
          disabled={deleteRecord.isPending}
          onClick={confirm}
        >
          {deleteRecord.isPending ? 'Removendo...' : 'Remover'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
