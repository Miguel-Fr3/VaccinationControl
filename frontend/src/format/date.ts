import type { IsoDate } from '../api/types';

function pad(value: number): string {
  return String(value).padStart(2, '0');
}

function isoDate(year: number, month: number, day: number): IsoDate {
  return `${String(year)}-${pad(month)}-${pad(day)}`;
}

/**
 * O dia de hoje que a API aceita: o menor entre a data local e a UTC. O validator do backend
 * compara a data de aplicação com `DateTime.UtcNow`, e a leste de Greenwich a data local passa
 * na frente dela — o seletor ofereceria, e o formulário já viria preenchido com, um dia que
 * volta como 400.
 */
export function today(): IsoDate {
  const now = new Date();

  const local = isoDate(now.getFullYear(), now.getMonth() + 1, now.getDate());
  const utc = isoDate(now.getUTCFullYear(), now.getUTCMonth() + 1, now.getUTCDate());

  // Comparar texto ISO é comparar cronologia: o formato é ordenável.
  return local < utc ? local : utc;
}

export function formatIsoDate(value: IsoDate): string {
  return value.split('-').reverse().join('/');
}
