import { useQuery } from '@tanstack/react-query';

import { api } from '../../api/client';
import type { VaccinationCard } from '../../api/types';

export const vaccinationCardKey = 'vaccination-card';

/**
 * Cartão de vacinação de uma pessoa. Fica aqui, e não em `people/`, para o cartão ter uma
 * chave de cache só — a tela de pessoas o consulta para contar o que a exclusão apaga.
 */
export function useVaccinationCard(personId: string | undefined, enabled = true) {
  return useQuery({
    queryKey: [vaccinationCardKey, personId],
    queryFn: async () => {
      const { data } = await api.get<VaccinationCard>(`/api/people/${personId!}/vaccination-card`);
      return data;
    },
    enabled: enabled && !!personId,
  });
}

/** Quantas aplicações o cartão tem ao todo, somando as de todas as vacinas. */
export function countDoses(card: VaccinationCard | undefined): number {
  return card?.vaccines.reduce((total, vaccine) => total + vaccine.totalDoses, 0) ?? 0;
}
