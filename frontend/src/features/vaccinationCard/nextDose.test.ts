import { describe, expect, it } from 'vitest';

import type { VaccinationCard, VaccinationType } from '../../api/types';
import { allowsBooster, suggestDose } from './nextDose';

type TestDose = { type: VaccinationType; number: number; date: string };

/** Cartão com uma vacina só, montado a partir das aplicações informadas. */
function cardWith(doses: TestDose[], vaccineId = 'v1'): VaccinationCard {
  return {
    personId: 'p1',
    personName: 'Maria Silva',
    document: '12345678901',
    vaccines: [
      {
        vaccineId,
        vaccineName: 'BCG',
        totalDoses: doses.length,
        doses: doses.map((dose, index) => ({
          recordId: `r${String(index)}`,
          vaccinationType: dose.type,
          doseNumber: dose.number,
          vaccinationDate: dose.date,
        })),
      },
    ],
  };
}

describe('suggestDose', () => {
  it('sugere a dose 1 para vacina sem aplicacao', () => {
    expect(suggestDose(cardWith([]), 'v1', 'Dose')).toEqual({
      doseNumber: 1,
      previousDate: undefined,
    });
  });

  it('sugere a dose 1 para vacina que nao esta no cartao', () => {
    expect(suggestDose(cardWith([]), 'outra-vacina', 'Dose').doseNumber).toBe(1);
  });

  it('sugere a dose 1 quando ainda nao ha cartao carregado', () => {
    expect(suggestDose(undefined, 'v1', 'Dose').doseNumber).toBe(1);
  });

  it('sugere a proxima da sequencia e a data da anterior', () => {
    const card = cardWith([
      { type: 'Dose', number: 1, date: '2024-01-10' },
      { type: 'Dose', number: 2, date: '2024-02-20' },
    ]);

    expect(suggestDose(card, 'v1', 'Dose')).toEqual({
      doseNumber: 3,
      previousDate: '2024-02-20',
    });
  });

  it('conta cada tipo em sequencia propria', () => {
    // Quem tem as doses normais 1 e 2 tem o próximo reforço no número 1, não no 3.
    const card = cardWith([
      { type: 'Dose', number: 1, date: '2024-01-10' },
      { type: 'Dose', number: 2, date: '2024-02-20' },
    ]);

    expect(suggestDose(card, 'v1', 'BoosterDose').doseNumber).toBe(1);
    expect(suggestDose(card, 'v1', 'Dose').doseNumber).toBe(3);
  });

  it('nao mistura a data da anterior entre os tipos', () => {
    const card = cardWith([
      { type: 'Dose', number: 1, date: '2024-01-10' },
      { type: 'BoosterDose', number: 1, date: '2024-06-01' },
    ]);

    expect(suggestDose(card, 'v1', 'Dose').previousDate).toBe('2024-01-10');
    expect(suggestDose(card, 'v1', 'BoosterDose').previousDate).toBe('2024-06-01');
  });

  it('usa o maior numero, e nao a quantidade', () => {
    // Com a dose 2 removida, o próximo continua sendo o 3 — contar daria 2 e colidiria.
    const card = cardWith([
      { type: 'Dose', number: 1, date: '2024-01-10' },
      { type: 'Dose', number: 3, date: '2024-03-30' },
    ]);

    expect(suggestDose(card, 'v1', 'Dose').doseNumber).toBe(4);
  });
});

describe('allowsBooster', () => {
  it('nao permite reforco sem dose normal', () => {
    expect(allowsBooster(cardWith([]), 'v1')).toBe(false);
  });

  it('permite reforco depois de uma dose normal', () => {
    expect(allowsBooster(cardWith([{ type: 'Dose', number: 1, date: '2024-01-10' }]), 'v1')).toBe(
      true,
    );
  });

  it('olha so a vacina escolhida', () => {
    // A dose normal de outra vacina não habilita o reforço desta.
    const card = cardWith([{ type: 'Dose', number: 1, date: '2024-01-10' }], 'v1');

    expect(allowsBooster(card, 'v2')).toBe(false);
  });
});
