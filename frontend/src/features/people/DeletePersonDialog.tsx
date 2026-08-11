import { useState } from 'react';
import {
  Alert,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Stack,
} from '@mui/material';

import { errorMessage } from '../../api/problemDetails';
import type { Person } from '../../api/types';
import { countDoses, useVaccinationCard } from '../vaccinationCard/useVaccinationCard';
import { useDeletePerson } from './usePeople';

type DeletePersonDialogProps = {
  person: Person;
  onClose: () => void;
};

/**
 * Confirmação da exclusão. O DELETE é em cascata no banco, então o aviso precisa dizer que
 * o cartão vai junto — e quantas aplicações isso representa.
 */
export function DeletePersonDialog({ person, onClose }: DeletePersonDialogProps) {
  const [alertMessage, setAlertMessage] = useState<string | null>(null);
  const card = useVaccinationCard(person.id);
  const deletePerson = useDeletePerson();

  const doses = countDoses(card.data);

  const confirm = () => {
    setAlertMessage(null);

    deletePerson.mutate(person.id, {
      onSuccess: onClose,
      onError: error => {
        setAlertMessage(errorMessage(error));
      },
    });
  };

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>Excluir pessoa</DialogTitle>

      <DialogContent>
        <Stack spacing={2}>
          <DialogContentText>
            Excluir <strong>{person.name}</strong> apaga também o cartão de vacinação dela. Esta
            ação não pode ser desfeita.
          </DialogContentText>

          {card.isPending && (
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              <CircularProgress size={16} />
              <DialogContentText>Verificando o cartão...</DialogContentText>
            </Stack>
          )}

          {card.isError && (
            // Sem o número, o aviso de cascata continua valendo — só não dá para dimensioná-lo.
            <Alert severity="warning">
              Não foi possível verificar o cartão. Se houver registros de vacinação, eles serão
              apagados junto.
            </Alert>
          )}

          {card.isSuccess && (
            <Alert severity={doses > 0 ? 'warning' : 'info'}>
              {doses === 0 && 'Esta pessoa não tem nenhum registro de vacinação.'}
              {doses === 1 && '1 registro de vacinação será perdido.'}
              {doses > 1 && `${String(doses)} registros de vacinação serão perdidos.`}
            </Alert>
          )}

          {alertMessage && <Alert severity="error">{alertMessage}</Alert>}
        </Stack>
      </DialogContent>

      <DialogActions>
        <Button onClick={onClose} disabled={deletePerson.isPending}>
          Cancelar
        </Button>
        <Button
          color="error"
          variant="contained"
          // Confirmar antes de saber o tamanho do estrago é o que este diálogo existe para evitar.
          disabled={card.isPending || deletePerson.isPending}
          onClick={confirm}
        >
          {deletePerson.isPending ? 'Excluindo...' : 'Excluir'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
