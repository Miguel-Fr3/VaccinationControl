/** Só os dígitos, limitados aos 11 que a API aceita. É o formato que vai no corpo. */
export function stripCpf(value: string): string {
  return value.replace(/\D/g, '').slice(0, 11);
}

/** Aplica a máscara 000.000.000-00, inclusive em valor parcial enquanto se digita. */
export function formatCpf(value: string): string {
  return stripCpf(value)
    .replace(/^(\d{3})(\d)/, '$1.$2')
    .replace(/^(\d{3})\.(\d{3})(\d)/, '$1.$2.$3')
    .replace(/^(\d{3})\.(\d{3})\.(\d{3})(\d)/, '$1.$2.$3-$4');
}

/**
 * Termo de busca. O mesmo campo procura nome e CPF, e o CPF está gravado sem máscara: se o
 * que foi digitado só tem dígitos e pontuação, vai limpo — senão "123.456" não acharia nada.
 */
export function searchTerm(value: string): string {
  return /^[\d.\-\s]+$/.test(value) ? stripCpf(value) : value;
}
