/// <reference types="vite/client" />

// Tipa as variáveis do `.env`, que por padrão chegam como `any`.
interface ImportMetaEnv {
  /** Endereço base da API. Veja o `.env.example`. */
  readonly VITE_API_URL: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
