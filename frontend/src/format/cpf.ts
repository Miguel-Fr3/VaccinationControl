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

/** Dígito verificador dos `length` primeiros dígitos: pesos de `length + 1` até 2. */
function checkDigit(digits: string, length: number): string {
  let sum = 0;

  for (let position = 0; position < length; position++) {
    sum += Number(digits[position]) * (length + 1 - position);
  }

  const remainder = sum % 11;

  return String(remainder < 2 ? 0 : 11 - remainder);
}

/**
 * Se os onze dígitos formam um CPF possível. Espelha o `Cpf.IsValid` do backend, que continua
 * sendo a autoridade — aqui a conta serve para avisar antes do envio, sem a ida ao servidor.
 *
 * É a única regra duplicada de propósito entre os dois lados, e cabe porque não é regra de
 * negócio deste sistema: o cálculo é fixo, definido fora dele, e não muda com o produto.
 */
export function isValidCpf(value: string): boolean {
  const digits = stripCpf(value);

  if (digits.length !== 11) {
    return false;
  }

  // Sequência de um dígito só fecha a aritmética e não é CPF de ninguém.
  if (digits.split('').every(digit => digit === digits[0])) {
    return false;
  }

  return checkDigit(digits, 9) === digits[9] && checkDigit(digits, 10) === digits[10];
}

/**
 * Termo de busca. O mesmo campo procura nome e CPF, e o CPF está gravado sem máscara: se o
 * que foi digitado só tem dígitos e pontuação, vai limpo — senão "123.456" não acharia nada.
 */
export function searchTerm(value: string): string {
  return /^[\d.\-\s]+$/.test(value) ? stripCpf(value) : value;
}
