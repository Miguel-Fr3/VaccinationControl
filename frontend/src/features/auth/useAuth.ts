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
    // `onSettled`, e não `onSuccess`: se a chamada falhar por rede, o usuário clicou em sair e
    // continuaria na aplicação, sem sessão encerrada e sem aviso nenhum. Encerrar do lado do
    // cliente é o que está ao alcance daqui — o cookie pode sobreviver, mas ele é `HttpOnly` e
    // a próxima requisição que o servidor recusar derruba a sessão pelo tratador de 401.
    onSettled: () => {
      setUser(null);

      // Descarta o cache das outras telas, mas nunca o da sessão: remover uma query que
      // está sendo observada desliga o provider dela, e a tela para de reagir ao logout.
      queryClient.removeQueries({
        predicate: query => query.queryKey[0] !== sessionQueryKey[0],
      });
    },
  });
}
