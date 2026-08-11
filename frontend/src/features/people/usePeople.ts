import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { api } from '../../api/client';
import type { CreatePersonRequest, ListQuery, PagedResult, Person } from '../../api/types';
import { vaccinationCardKey } from '../vaccinationCard/useVaccinationCard';

const peopleKey = 'people';

// Lista as pessoas. A busca cobre nome e documento, e é do servidor.
export function usePeople(query: ListQuery) {
  return useQuery({
    queryKey: [peopleKey, query],
    queryFn: async () => {
      const { data } = await api.get<PagedResult<Person>>('/api/people', { params: query });
      return data;
    },
    placeholderData: keepPreviousData,
  });
}

// Cadastra uma pessoa e recarrega a listagem.
export function useCreatePerson() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (body: CreatePersonRequest) => {
      const { data } = await api.post<Person>('/api/people', body);
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: [peopleKey] });
    },
  });
}

// Exclui uma pessoa. O DELETE é em cascata: o cartão dela vai junto, no banco.
export function useDeletePerson() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (personId: string) => api.delete(`/api/people/${personId}`),
    onSuccess: (_, personId) => {
      void queryClient.invalidateQueries({ queryKey: [peopleKey] });
      // O cartão em cache não existe mais do outro lado; mantê-lo mostraria dado fantasma.
      queryClient.removeQueries({ queryKey: [vaccinationCardKey, personId] });
    },
  });
}
