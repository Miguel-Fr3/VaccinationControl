/// <reference types="vitest/config" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  test: {
    // jsdom porque os testes montam componentes; sem DOM não há o que consultar.
    environment: 'jsdom',
    // Sem globais: cada teste importa `describe`/`it`/`expect` do vitest, e o `tsc -b` do
    // build confere os arquivos de teste junto com o resto de `src/`.
    setupFiles: ['./src/test/setup.ts'],
    css: false,
  },
});
