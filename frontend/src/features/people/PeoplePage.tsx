import { useState } from 'react';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  IconButton,
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
  Tooltip,
  Typography,
} from '@mui/material';
import DeleteOutlinedIcon from '@mui/icons-material/DeleteOutlined';

import { errorMessage } from '../../api/problemDetails';
import type { Person } from '../../api/types';
import { useListQuery } from '../../hooks/useListQuery';
import { formatCpf, searchTerm } from '../../format/cpf';
import { DeletePersonDialog } from './DeletePersonDialog';
import { PersonDialog } from './PersonDialog';
import { usePeople } from './usePeople';

export default function PeoplePage() {
  const list = useListQuery();
  const [dialogOpen, setDialogOpen] = useState(false);
  const [personToDelete, setPersonToDelete] = useState<Person | null>(null);

  const { data, isPending, isFetching, isError, error } = usePeople({
    ...list.query,
    // O CPF está gravado sem máscara: um "123.456" digitado precisa chegar como "123456".
    search: list.query.search ? searchTerm(list.query.search) : undefined,
  });

  // Com keepPreviousData o `data` anterior sobrevive ao erro, e a tabela exibida não
  // corresponderia ao filtro digitado.
  const showList = !isError && data;

  return (
    <Stack spacing={3}>
      <Stack
        direction="row"
        spacing={2}
        sx={{ alignItems: 'center', justifyContent: 'space-between' }}
      >
        <Typography variant="h1">Pessoas</Typography>

        <Button
          variant="contained"
          onClick={() => {
            setDialogOpen(true);
          }}
        >
          Nova pessoa
        </Button>
      </Stack>

      <TextField
        label="Buscar por nome ou CPF"
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
            ? `Nenhuma pessoa encontrada para "${list.query.search}".`
            : 'Nenhuma pessoa cadastrada ainda. Comece cadastrando a primeira.'}
        </Alert>
      )}

      {showList && data.items.length > 0 && (
        <Paper sx={{ opacity: isFetching ? 0.6 : 1, transition: 'opacity 150ms' }}>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Nome</TableCell>
                  <TableCell>CPF</TableCell>
                  <TableCell align="right">Ações</TableCell>
                </TableRow>
              </TableHead>

              <TableBody>
                {data.items.map(person => (
                  <TableRow key={person.id} hover>
                    <TableCell>{person.name}</TableCell>
                    <TableCell>{formatCpf(person.document)}</TableCell>
                    <TableCell align="right">
                      <Tooltip title="Excluir">
                        <IconButton
                          color="error"
                          aria-label={`Excluir ${person.name}`}
                          onClick={() => {
                            setPersonToDelete(person);
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

          <TablePagination
            component="div"
            count={data.totalCount}
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

      <PersonDialog
        open={dialogOpen}
        onClose={() => {
          setDialogOpen(false);
        }}
      />

      {personToDelete && (
        <DeletePersonDialog
          person={personToDelete}
          onClose={() => {
            setPersonToDelete(null);
          }}
        />
      )}
    </Stack>
  );
}
