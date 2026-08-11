import { useState } from 'react';
import {
  Alert,
  Autocomplete,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Stack,
  TextField,
} from '@mui/material';
import { Controller, useForm, useWatch } from 'react-hook-form';

import { applyValidationErrors, errorMessage } from '../../api/problemDetails';
import type {
  RegisterVaccinationRequest,
  Vaccine,
  VaccinationCard,
  VaccinationType,
} from '../../api/types';
import { vaccinationTypeLabels } from '../../api/types';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import { useVaccines } from '../vaccines/useVaccines';
import { today } from '../../format/date';
import { allowsBooster, suggestDose } from './nextDose';
import { useRegisterVaccination } from './useVaccinationCard';

type VaccinationDialogProps = {
  personId: string;
  card: VaccinationCard | undefined;
  open: boolean;
  onClose: () => void;
};

/**
 * Registro de aplicação. O formulário **guia** — sugere o próximo número do tipo, limita a
 * data e desabilita reforço sem dose normal —, mas quem recusa de fato são as RN05 a RN08
 * no handler da API. Reimplementá-las aqui criaria uma segunda fonte de verdade.
 */
export function VaccinationDialog({ personId, card, open, onClose }: VaccinationDialogProps) {
  const [alertMessage, setAlertMessage] = useState<string | null>(null);
  const registerVaccination = useRegisterVaccination(personId);

  // O catálogo é buscado no servidor, e não carregado inteiro: um `pageSize` alto e a
  // busca por trecho mantêm a lista utilizável independentemente do tamanho do cadastro.
  const [vaccineSearch, setVaccineSearch] = useState('');
  const [selectedVaccine, setSelectedVaccine] = useState<Vaccine | null>(null);
  const debouncedVaccineSearch = useDebouncedValue(vaccineSearch);

  const vaccines = useVaccines({
    search: debouncedVaccineSearch || undefined,
    page: 1,
    pageSize: 50,
  });

  const {
    control,
    register,
    handleSubmit,
    setError,
    setValue,
    reset,
    formState: { errors },
  } = useForm<RegisterVaccinationRequest>({
    defaultValues: {
      vaccineId: '',
      vaccinationType: 'Dose',
      doseNumber: 1,
      vaccinationDate: today(),
    },
  });

  // O `useWatch` é necessário para reagir a mudanças de vacina ou tipo e atualizar a sugestão.
  const vaccineId = useWatch({ control, name: 'vaccineId' });
  const vaccinationType = useWatch({ control, name: 'vaccinationType' });

  const suggestion = suggestDose(card, vaccineId, vaccinationType);
  const boosterAllowed = allowsBooster(card, vaccineId);

  const applySuggestion = (nextVaccineId: string, nextType: VaccinationType) => {
    setValue('doseNumber', suggestDose(card, nextVaccineId, nextType).doseNumber);
  };

  const close = () => {
    reset({
      vaccineId: '',
      vaccinationType: 'Dose',
      doseNumber: 1,
      vaccinationDate: today(),
    });
    setSelectedVaccine(null);
    setVaccineSearch('');
    setAlertMessage(null);
    onClose();
  };

  const submit = handleSubmit(body => {
    setAlertMessage(null);

    registerVaccination.mutate(body, {
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
        <DialogTitle>Registrar aplicação</DialogTitle>

        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {alertMessage && <Alert severity="error">{alertMessage}</Alert>}

            <Controller
              name="vaccineId"
              control={control}
              render={({ field }) => (
                <Autocomplete
                  options={vaccines.data?.items ?? []}
                  value={selectedVaccine}
                  loading={vaccines.isFetching}
                  getOptionLabel={vaccine => vaccine.name}
                  isOptionEqualToValue={(option, value) => option.id === value.id}
                  // A busca é do servidor: sem isto o componente filtraria de novo, em
                  // memória, e esconderia resultados que a API já devolveu.
                  filterOptions={options => options}
                  onInputChange={(_, value) => {
                    setVaccineSearch(value);
                  }}
                  onChange={(_, vaccine) => {
                    setSelectedVaccine(vaccine);
                    field.onChange(vaccine?.id ?? '');
                    applySuggestion(vaccine?.id ?? '', vaccinationType);
                  }}
                  noOptionsText="Nenhuma vacina encontrada."
                  renderInput={params => (
                    <TextField
                      {...params}
                      label="Vacina"
                      error={!!errors.vaccineId}
                      helperText={errors.vaccineId?.message}
                    />
                  )}
                />
              )}
            />

            <Controller
              name="vaccinationType"
              control={control}
              render={({ field }) => (
                <TextField
                  select
                  label="Tipo"
                  fullWidth
                  value={field.value}
                  onChange={event => {
                    const type = event.target.value as VaccinationType;
                    field.onChange(type);
                    applySuggestion(vaccineId, type);
                  }}
                  error={!!errors.vaccinationType}
                  helperText={
                    errors.vaccinationType?.message ??
                    (vaccineId && !boosterAllowed
                      ? 'O reforço exige ao menos uma dose normal desta vacina.'
                      : undefined)
                  }
                >
                  <MenuItem value="Dose">{vaccinationTypeLabels.Dose}</MenuItem>
                  <MenuItem value="BoosterDose" disabled={!boosterAllowed}>
                    {vaccinationTypeLabels.BoosterDose}
                  </MenuItem>
                </TextField>
              )}
            />

            <TextField
              label="Número da dose"
              type="number"
              fullWidth
              slotProps={{ htmlInput: { min: 1 } }}
              error={!!errors.doseNumber}
              helperText={errors.doseNumber?.message}
              {...register('doseNumber', { valueAsNumber: true })}
            />

            <TextField
              label="Data de aplicação"
              type="date"
              fullWidth
              slotProps={{
                inputLabel: { shrink: true },
                htmlInput: { max: today(), min: suggestion.previousDate },
              }}
              error={!!errors.vaccinationDate}
              helperText={errors.vaccinationDate?.message}
              {...register('vaccinationDate')}
            />
          </Stack>
        </DialogContent>

        <DialogActions>
          <Button onClick={close} disabled={registerVaccination.isPending}>
            Cancelar
          </Button>
          <Button
            type="submit"
            variant="contained"
            disabled={!vaccineId || registerVaccination.isPending}
          >
            {registerVaccination.isPending ? 'Registrando...' : 'Registrar'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}
