import { useEffect, useMemo } from 'react';
import type { ReactNode } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';

import { api, registerSessionLostHandler } from '../api/client';
import { errorStatus } from '../api/problemDetails';
import type { Session } from '../api/types';
import { SessionContext, sessionQueryKey } from './SessionContext';
import type { SessionState } from './SessionContext';

/**
 * Descobre quem está logado e mantém a sessão disponível para a aplicação inteira. O
 * cookie é `HttpOnly`, então `GET /api/auth/me` é o único jeito de saber.
 */
export function SessionProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();

  const { data, isPending } = useQuery({
    queryKey: sessionQueryKey,
    queryFn: async () => {
      try {
        const { data } = await api.get<Session>('/api/auth/me');
        return data;
      } catch (error) {
        // 401 aqui é "ninguém logado", não falha: vira sessão nula.
        if (errorStatus(error) === 401) {
          return null;
        }

        throw error;
      }
    },
  });

  useEffect(
    // O cliente avisa quando um 401 derruba a sessão e devolve o cancelamento do registro.
    () => registerSessionLostHandler(() => queryClient.setQueryData(sessionQueryKey, null)),
    [queryClient],
  );

  const session = useMemo<SessionState>(
    () => ({
      user: data ?? null,
      isLoading: isPending,
      setUser: user => queryClient.setQueryData(sessionQueryKey, user),
    }),
    [data, isPending, queryClient],
  );

  return <SessionContext value={session}>{children}</SessionContext>;
}
