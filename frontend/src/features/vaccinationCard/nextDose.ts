import type { IsoDate, VaccinationCard, VaccinationType } from '../../api/types';

/** As aplicações de uma vacina no cartão, restritas a um tipo. */
function dosesOf(
  card: VaccinationCard | undefined,
  vaccineId: string,
  type: VaccinationType,
) {
  return (
    card?.vaccines
      .find(vaccine => vaccine.vaccineId === vaccineId)
      ?.doses.filter(dose => dose.vaccinationType === type) ?? []
  );
}

export type DoseSuggestion = {
  doseNumber: number;
  previousDate: IsoDate | undefined;
};

/**
 * Sugestão para o formulário: o próximo número livre do tipo e a data da última aplicação
 * dele, que a nova não pode anteceder.
 *
 * A numeração é independente por tipo — quem tem as doses normais 1 e 2 tem o próximo
 * reforço no número 1, não no 3. E a conta usa o maior número, não a quantidade: com a
 * dose 2 removida, o próximo continua sendo o 3.
 *
 * É sugestão, não validação: quem decide são as RN05 a RN07, no handler da API.
 */
export function suggestDose(
  card: VaccinationCard | undefined,
  vaccineId: string,
  type: VaccinationType,
): DoseSuggestion {
  const doses = dosesOf(card, vaccineId, type);

  const last = doses.reduce<(typeof doses)[number] | undefined>(
    (maior, dose) => (maior && maior.doseNumber >= dose.doseNumber ? maior : dose),
    undefined,
  );

  return {
    doseNumber: (last?.doseNumber ?? 0) + 1,
    previousDate: last?.vaccinationDate,
  };
}

/**
 * Se o reforço faz sentido para esta vacina. Espelha a RN08 apenas para desabilitar a
 * opção; recusar de verdade continua sendo da API.
 */
export function allowsBooster(card: VaccinationCard | undefined, vaccineId: string): boolean {
  return dosesOf(card, vaccineId, 'Dose').length > 0;
}
