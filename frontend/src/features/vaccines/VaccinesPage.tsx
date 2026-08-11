import { useState } from 'react';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';

import { errorMessage } from '../../api/problemDetails';
import { useListQuery } from '../../hooks/useListQuery';
import { VaccineDialog } from './VaccineDialog';
import { useVaccines } from './useVaccines';

export default function VaccinesPage() {
  const list = useListQuery();
  const [dialogOpen, setDialogOpen] = useState(false);

  const { data, isPending, isFetching, isError, error } = useVaccines(list.query);

  // Com keepPreviousData o `data` da consulta anterior sobrevive ao erro. Exibir os dois
  // mostraria uma tabela que não corresponde ao filtro digitado.
  const showList = !isError && data;

  return (
    <Stack spacing={3}>
      <Stack
        direction="row"
        spacing={2}
        sx={{ alignItems: 'center', justifyContent: 'space-between' }}
      >
        <Typography variant="h1">Vacinas</Typography>

        <Button
          variant="contained"
          onClick={() => {
            setDialogOpen(true);
          }}
        >
          Nova vacina
        </Button>
      </Stack>

      <TextField
        label="Buscar por nome"
        value={list.search}
        onChange={event => {
          list.setSearch(event.target.value);
        }}
        fullWidth
      />

      {isPending && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
          <CircularProgress />
        </Box>
      )}

      {isError && <Alert severity="error">{errorMessage(error)}</Alert>}

      {showList && data.items.length === 0 && (
        <Alert severity="info">
          {list.query.search
            ? `Nenhuma vacina encontrada para "${list.query.search}".`
            : 'Nenhuma vacina cadastrada ainda. Comece cadastrando a primeira.'}
        </Alert>
      )}

      {showList && data.items.length > 0 && (
        // Enquanto a próxima página carrega, a atual continua visível — esmaecida, para
        // não passar por dado atualizado.
        <Paper sx={{ opacity: isFetching ? 0.6 : 1, transition: 'opacity 150ms' }}>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Nome</TableCell>
                </TableRow>
              </TableHead>

              <TableBody>
                {data.items.map(vaccine => (
                  <TableRow key={vaccine.id} hover>
                    <TableCell>{vaccine.name}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>

          <TablePagination
            component="div"
            count={data.totalCount}
            // A API conta as páginas a partir de 1; a tabela do MUI, a partir de 0.
            page={list.page - 1}
            onPageChange={(_, nextPage) => {
              list.setPage(nextPage + 1);
            }}
            rowsPerPage={list.pageSize}
            onRowsPerPageChange={event => {
              list.setPageSize(Number(event.target.value));
            }}
            rowsPerPageOptions={[10, 20, 50]}
          />
        </Paper>
      )}

      <VaccineDialog
        open={dialogOpen}
        onClose={() => {
          setDialogOpen(false);
        }}
      />
    </Stack>
  );
}
