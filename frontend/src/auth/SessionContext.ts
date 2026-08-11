import { createContext } from 'react';

import type { Session } from '../api/types';

/** Chave do cache onde a sessão corrente é guardada. */
export const sessionQueryKey = ['session'];

/** Sessão corrente e o que as telas usam para alterá-la. */
export type SessionState = {
  user: Session | null;
  isLoading: boolean;
  setUser: (user: Session | null) => void;
};

export const SessionContext = createContext<SessionState | null>(null);
