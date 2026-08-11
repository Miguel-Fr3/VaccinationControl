import type { ReactElement, ReactNode } from 'react';
import { ThemeProvider } from '@mui/material';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

import { theme } from '../theme';

/**
 * Monta o componente com os mesmos provedores do `main.tsx`. O QueryClient é novo a cada
 * chamada: um cache compartilhado faria um teste enxergar a resposta simulada do anterior.
 */
export function renderWithProviders(ui: ReactElement, initialRoute = '/') {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  function Providers({ children }: { children: ReactNode }) {
    return (
      <ThemeProvider theme={theme}>
        <QueryClientProvider client={queryClient}>
          <MemoryRouter initialEntries={[initialRoute]}>{children}</MemoryRouter>
        </QueryClientProvider>
      </ThemeProvider>
    );
  }

  return { queryClient, ...render(ui, { wrapper: Providers }) };
}
