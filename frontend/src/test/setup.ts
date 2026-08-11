import '@testing-library/jest-dom/vitest';
import { afterAll, afterEach, beforeAll } from 'vitest';
import { cleanup } from '@testing-library/react';

import { server } from './server';

// `onUnhandledRequest: 'error'` transforma uma rota não simulada em falha do teste, em vez
// de uma requisição real saindo para a máquina de quem roda a suíte.
beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' });
});

afterEach(() => {
  cleanup();
  // Descarta os handlers que um teste tenha sobrescrito, para não vazarem para o próximo.
  server.resetHandlers();
});

afterAll(() => {
  server.close();
});
