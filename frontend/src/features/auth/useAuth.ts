import { useMutation, useQueryClient } from '@tanstack/react-query';

import { api } from '../../api/client';
import type { Credentials, Session } from '../../api/types';
import { sessionQueryKey } from '../../auth/SessionContext';
import { useSession } from '../../auth/useSession';

/** Autentica e abre a sessão. O cookie vem no `Set-Cookie` da resposta. */
export function useLogin() {
  const { setUser } = useSession();

  return useMutation({
    mutationFn: async (credentials: Credentials) => {
      const { data } = await api.post<Session>('/api/auth/login', credentials);
      return data;
    },
    onSuccess: user => {
      setUser(user);
    },
  });
}

/** Cadastra o usuário e já abre a sessão. */
export function useRegister() {
  const { setUser } = useSession();

  return useMutation({
    mutationFn: async (credentials: Credentials) => {
      const { data } = await api.post<Session>('/api/auth/register', credentials);
      return data;
    },
    onSuccess: user => {
      setUser(user);
    },
  });
}

/** Encerra a sessão no servidor, que é quem apaga o cookie `HttpOnly`. */
export function useLogout() {
  const queryClient = useQueryClient();
  const { setUser } = useSession();

  return useMutation({
    mutationFn: () => api.post('/api/auth/logout'),
    onSuccess: () => {
      setUser(null);

      // Descarta o cache das outras telas, mas nunca o da sessão: remover uma query que
      // está sendo observada desliga o provider dela, e a tela para de reagir ao logout.
      queryClient.removeQueries({
        predicate: query => query.queryKey[0] !== sessionQueryKey[0],
      });
    },
  });
}
