import type { IsoDate } from '../api/types';

function pad(value: number): string {
  return String(value).padStart(2, '0');
}

export function today(): IsoDate {
  const now = new Date();

  return `${String(now.getFullYear())}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}

export function formatIsoDate(value: IsoDate): string {
  return value.split('-').reverse().join('/');
}
