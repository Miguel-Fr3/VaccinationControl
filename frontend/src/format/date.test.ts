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
  });

  it('usa a data local, e nao a UTC', () => {
    // 23h de 10/01 em Brasília já é 11/01 em UTC. O seletor precisa parar no 10.
    vi.useFakeTimers();
    vi.setSystemTime(new Date(2024, 0, 10, 23, 30));

    expect(today()).toBe('2024-01-10');
  });

  it('preenche mes e dia com zero a esquerda', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date(2024, 1, 5, 12, 0));

    expect(today()).toBe('2024-02-05');
  });
});
