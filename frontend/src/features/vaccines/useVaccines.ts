import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { api } from '../../api/client';
import type { CreateVaccineRequest, ListQuery, PagedResult, Vaccine } from '../../api/types';

const vaccinesKey = 'vaccines';

// Lista as vacinas.
export function useVaccines(query: ListQuery) {
  return useQuery({
    queryKey: [vaccinesKey, query],
    queryFn: async () => {
      const { data } = await api.get<PagedResult<Vaccine>>('/api/vaccines', { params: query });
      return data;
    },
    // Mantém os dados anteriores enquanto a nova página é carregada, evitando que a tabela fique vazia.
    placeholderData: keepPreviousData,
  });
}

// Cadastra uma vacina e recarrega a listagem.
export function useCreateVaccine() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (body: CreateVaccineRequest) => {
      const { data } = await api.post<Vaccine>('/api/vaccines', body);
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: [vaccinesKey] });
    },
  });
}

// Exclui uma vacina do catálogo. Sem cascata: a API responde 409 se houver dose registrada,
// e a mensagem dela é o que a tela mostra — quem decide é o servidor.
export function useDeleteVaccine() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (vaccineId: string) => api.delete(`/api/vaccines/${vaccineId}`),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: [vaccinesKey] });
    },
  });
}
