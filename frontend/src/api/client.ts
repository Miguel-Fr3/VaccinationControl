import axios from 'axios';
import type { AxiosError } from 'axios';

/** Cliente HTTP da API. `withCredentials` envia o cookie de sessão em outra origem. */
export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  withCredentials: true,
});

type SessionLostHandler = () => void;

let sessionLostHandler: SessionLostHandler | null = null;

/** Registra o tratador de sessão perdida e devolve a função que desfaz o registro. */
export function registerSessionLostHandler(handler: SessionLostHandler): () => void {
  sessionLostHandler = handler;

  return () => {
    // Só limpa se ainda for o tratador atual.
    if (sessionLostHandler === handler) {
      sessionLostHandler = null;
    }
  };
}

/** Indica se a URL é de uma rota de autenticação, onde o 401 é resposta esperada. */
function isAuthRoute(url: string | undefined): boolean {
  return url?.includes('/api/auth/') ?? false;
}

api.interceptors.response.use(
  response => response,
  (error: AxiosError) => {
    // 401 fora das rotas de autenticação: sessão ausente ou expirada.
    if (error.response?.status === 401 && !isAuthRoute(error.config?.url)) {
      sessionLostHandler?.();
    }

    return Promise.reject(error);
  },
);
