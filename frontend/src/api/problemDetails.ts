import { isAxiosError } from 'axios';
import type { FieldValues, Path, UseFormSetError } from 'react-hook-form';

/** Corpo de erro da API (RFC 9457). `errors` só aparece no 400 de validação. */
export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
};

const DEFAULT_MESSAGE = 'Não foi possível concluir a operação.';

const NO_RESPONSE_MESSAGE =
  'Não foi possível falar com o servidor. Verifique sua conexão e tente de novo.';

/** Extrai o corpo do erro. Devolve `null` quando não é uma resposta JSON da API. */
export function extractProblemDetails(error: unknown): ProblemDetails | null {
  if (!isAxiosError(error)) {
    return null;
  }

  const body: unknown = error.response?.data;

  if (typeof body !== 'object' || body === null) {
    return null;
  }

  return body;
}

/** Status HTTP da resposta, quando houve uma. */
export function errorStatus(error: unknown): number | undefined {
  return isAxiosError(error) ? error.response?.status : undefined;
}

/** Mensagem para exibir: o `detail` da API, o aviso de rede ou o `fallback`. */
export function errorMessage(error: unknown, fallback: string = DEFAULT_MESSAGE): string {
  const problem = extractProblemDetails(error);

  if (problem?.detail) {
    return problem.detail;
  }

  // Sem resposta é rede ou API fora do ar.
  if (isAxiosError(error) && !error.response) {
    return NO_RESPONSE_MESSAGE;
  }

  return fallback;
}

/** Aplica os erros de validação nos campos do formulário. `false` se não houver nenhum. */
export function applyValidationErrors<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>,
): boolean {
  const errors = extractProblemDetails(error)?.errors;

  if (!errors) {
    return false;
  }

  let applied = false;

  for (const [field, messages] of Object.entries(errors)) {
    const message = messages[0];

    if (!message) {
      continue;
    }

    // 'Name' → 'name': a API devolve a chave em PascalCase; o campo do formulário é camelCase.
    const name = field.charAt(0).toLowerCase() + field.slice(1);

    setError(name as Path<T>, { type: 'server', message });
    applied = true;
  }

  return applied;
}
