import { useContext } from 'react';

import { SessionContext } from './SessionContext';
import type { SessionState } from './SessionContext';

/** Lê a sessão corrente. Falha fora do `SessionProvider`. */
export function useSession(): SessionState {
  const session = useContext(SessionContext);

  if (!session) {
    throw new Error('useSession precisa estar dentro de um SessionProvider.');
  }

  return session;
}
