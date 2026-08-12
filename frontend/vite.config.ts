/// <reference types="vitest/config" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  // Ancorado no diretório do projeto: sem isto o cache do Vitest é resolvido a partir do
  // arquivo que iniciou a execução e acaba gravado em `src/node_modules/`, dentro do código.
  cacheDir: 'node_modules/.vite',
  test: {
    // jsdom porque os testes montam componentes; sem DOM não há o que consultar.
    environment: 'jsdom',
    // Sem globais: cada teste importa `describe`/`it`/`expect` do vitest, e o `tsc -b` do
    // build confere os arquivos de teste junto com o resto de `src/`.
    setupFiles: ['./src/test/setup.ts'],
    css: false,
    coverage: {
      // `include` explícito porque o padrão mede só os arquivos que algum teste importou:
      // uma tela sem teste nenhum sairia do relatório em vez de aparecer com 0%.
      include: ['src/**/*.{ts,tsx}'],
      // O ponto de entrada, os helpers de teste e os próprios testes não dizem nada sobre
      // o quanto do produto está coberto.
      exclude: ['src/main.tsx', 'src/vite-env.d.ts', 'src/test/**', '**/*.test.{ts,tsx}'],
    },
  },
});
