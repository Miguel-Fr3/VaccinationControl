import { setupServer } from 'msw/node';

/** API simulada. Cada teste declara os handlers do cenário com `server.use(...)`. */
export const server = setupServer();
