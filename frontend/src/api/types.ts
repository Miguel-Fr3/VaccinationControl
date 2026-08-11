// Contratos da API, espelhando os DTOs do backend com os nomes do JSON.

/** Envelope das listagens. Sem paginação pedida, `pageSize` reflete o total. */
export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export type ListQuery = {
  search?: string;
  page?: number;
  pageSize?: number;
};

export type Session = {
  userId: string;
  email: string;
};

export type Credentials = {
  email: string;
  password: string;
};

export type Vaccine = {
  id: string;
  name: string;
};

export type CreateVaccineRequest = {
  name: string;
};

export type Person = {
  id: string;
  name: string;
  document: string;
};

export type CreatePersonRequest = {
  name: string;
  document: string;
};

export type VaccinationType = 'Dose' | 'BoosterDose';

/** Rótulos de exibição do tipo, espelhando o `Describe()` do backend. */
export const vaccinationTypeLabels: Record<VaccinationType, string> = {
  Dose: 'Dose',
  BoosterDose: 'Dose de reforço',
};

/** Data do `DateOnly` da API: "2024-01-10". Texto puro, sem hora e sem fuso. */
export type IsoDate = string;

export type VaccinationRecord = {
  id: string;
  personId: string;
  vaccineId: string;
  vaccineName: string;
  vaccinationType: VaccinationType;
  doseNumber: number;
  vaccinationDate: IsoDate;
};

// Corpo de `POST /api/people/{personId}/vaccinations` — o `personId` vem da rota.
export type RegisterVaccinationRequest = {
  vaccineId: string;
  vaccinationType: VaccinationType;
  doseNumber: number;
  vaccinationDate: IsoDate;
};

// Uma aplicação do cartão. O `recordId` é o que remove este registro específico.
export type VaccinationCardDose = {
  recordId: string;
  vaccinationType: VaccinationType;
  doseNumber: number;
  vaccinationDate: IsoDate;
};

export type VaccinationCardVaccine = {
  vaccineId: string;
  vaccineName: string;
  totalDoses: number;
  doses: VaccinationCardDose[];
};

// O cartão de uma pessoa: os registros dela agrupados por vacina.
export type VaccinationCard = {
  personId: string;
  personName: string;
  document: string;
  vaccines: VaccinationCardVaccine[];
};
