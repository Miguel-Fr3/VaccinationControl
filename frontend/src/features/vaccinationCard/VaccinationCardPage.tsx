import { useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  IconButton,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tooltip,
  Typography,
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import DeleteOutlinedIcon from '@mui/icons-material/DeleteOutlined';
import { Link as RouterLink, useParams } from 'react-router-dom';

import { errorMessage, errorStatus } from '../../api/problemDetails';
import type { VaccinationCardDose } from '../../api/types';
import { vaccinationTypeLabels } from '../../api/types';
import { formatCpf } from '../../format/cpf';
import { DeleteRecordDialog } from './DeleteRecordDialog';
import { VaccinationDialog } from './VaccinationDialog';
import { formatIsoDate } from '../../format/date';
import { useVaccinationCard } from './useVaccinationCard';

/** A aplicação escolhida para remoção, junto com a vacina a que pertence. */
type SelectedDose = { vaccineName: string; dose: VaccinationCardDose };

export default function VaccinationCardPage() {
  const { personId } = useParams<{ personId: string }>();
  const [dialogOpen, setDialogOpen] = useState(false);
  const [doseToDelete, setDoseToDelete] = useState<SelectedDose | null>(null);

  const { data: card, isPending, isError, error } = useVaccinationCard(personId);

  if (isPending) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 10 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (isError) {
    return (
      <Stack spacing={2} sx={{ alignItems: 'flex-start' }}>
        <Alert severity={errorStatus(error) === 404 ? 'warning' : 'error'}>
          {errorMessage(error)}
        </Alert>
        <Button component={RouterLink} to="/pessoas">
          Voltar para pessoas
        </Button>
      </Stack>
    );
  }

  return (
    <Stack spacing={3}>
      <Button
        component={RouterLink}
        to="/pessoas"
        startIcon={<ArrowBackIcon />}
        sx={{ alignSelf: 'flex-start' }}
      >
        Voltar para pessoas
      </Button>

      <Stack
        direction="row"
        spacing={2}
        sx={{ alignItems: 'flex-start', justifyContent: 'space-between' }}
      >
        <Stack>
          <Typography variant="h1">{card.personName}</Typography>
          <Typography variant="body2" color="text.secondary">
            CPF {formatCpf(card.document)}
          </Typography>
        </Stack>

        <Button
          variant="contained"
          onClick={() => {
            setDialogOpen(true);
          }}
        >
          Registrar aplicação
        </Button>
      </Stack>

      {card.vaccines.length === 0 && (
        <Alert severity="info">
          Nenhuma aplicação registrada ainda. Comece registrando a primeira.
        </Alert>
      )}

      {card.vaccines.map(vaccine => (
        <Paper key={vaccine.vaccineId}>
          <Stack
            direction="row"
            spacing={2}
            sx={{ alignItems: 'center', px: 2, py: 1.5 }}
          >
            <Typography variant="h2" sx={{ fontSize: '1.125rem' }}>
              {vaccine.vaccineName}
            </Typography>
            <Chip
              size="small"
              label={
                vaccine.totalDoses === 1 ? '1 aplicação' : `${String(vaccine.totalDoses)} aplicações`
              }
            />
          </Stack>

          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Tipo</TableCell>
                  <TableCell>Dose</TableCell>
                  <TableCell>Data</TableCell>
                  <TableCell align="right">Ações</TableCell>
                </TableRow>
              </TableHead>

              <TableBody>
                {vaccine.doses.map(dose => (
                  <TableRow key={dose.recordId} hover>
                    <TableCell>{vaccinationTypeLabels[dose.vaccinationType]}</TableCell>
                    <TableCell>{dose.doseNumber}</TableCell>
                    <TableCell>{formatIsoDate(dose.vaccinationDate)}</TableCell>
                    <TableCell align="right">
                      <Tooltip title="Remover">
                        <IconButton
                          color="error"
                          aria-label={`Remover ${vaccinationTypeLabels[dose.vaccinationType]} ${String(dose.doseNumber)} de ${vaccine.vaccineName}`}
                          onClick={() => {
                            setDoseToDelete({ vaccineName: vaccine.vaccineName, dose });
                          }}
                        >
                          <DeleteOutlinedIcon />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>
      ))}

      <VaccinationDialog
        personId={card.personId}
        card={card}
        open={dialogOpen}
        onClose={() => {
          setDialogOpen(false);
        }}
      />

      {doseToDelete && (
        <DeleteRecordDialog
          personId={card.personId}
          vaccineName={doseToDelete.vaccineName}
          dose={doseToDelete.dose}
          onClose={() => {
            setDoseToDelete(null);
          }}
        />
      )}
    </Stack>
  );
}
