import { afterEach, describe, expect, it, vi } from 'vitest';

import { formatIsoDate, today } from './date';

describe('formatIsoDate', () => {
  it('inverte a data da API para o formato brasileiro', () => {
    expect(formatIsoDate('2024-01-10')).toBe('10/01/2024');
  });

  it('nao desloca o dia', () => {
    // Passar por `new Date('2024-01-01')` e formatar de volta devolveria 31/12/2023 em
    // qualquer fuso a oeste de Greenwich.
    expect(formatIsoDate('2024-01-01')).toBe('01/01/2024');
  });
});

describe('today', () => {
  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllEnvs();
  });

  /** Fixa o fuso do processo e o instante, para o resultado não depender da máquina. */
  function freeze(timeZone: string, utcInstant: Date) {
    vi.stubEnv('TZ', timeZone);
    vi.useFakeTimers();
    vi.setSystemTime(utcInstant);
  }

  it('a oeste de Greenwich fica na data local, que ainda nao virou', () => {
    // 22h30 de 10/01 em Brasília já é 11/01 em UTC. O seletor precisa parar no 10: é o dia
    // que o usuário está vivendo, e a API aceita qualquer coisa até o 11.
    freeze('America/Sao_Paulo', new Date(Date.UTC(2024, 0, 11, 1, 30)));

    expect(today()).toBe('2024-01-10');
  });

  it('a leste de Greenwich para na data UTC, que e a referencia da API', () => {
    // 01h de 11/01 em Riade ainda é 10/01 em UTC, e o validator do backend compara com
    // `DateTime.UtcNow`: oferecer o 11 devolveria 400 num dia que já começou para o usuário.
    freeze('Asia/Riyadh', new Date(Date.UTC(2024, 0, 10, 22, 0)));

    expect(today()).toBe('2024-01-10');
  });

  it('preenche mes e dia com zero a esquerda', () => {
    freeze('America/Sao_Paulo', new Date(Date.UTC(2024, 1, 5, 15, 0)));

    expect(today()).toBe('2024-02-05');
  });
});
