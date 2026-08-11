import { describe, expect, it, vi } from 'vitest';
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';

import { server } from '../../test/server';
import { renderWithProviders } from '../../test/renderWithProviders';
import type { VaccinationCard } from '../../api/types';
import { VaccinationDialog } from './VaccinationDialog';

const catalog = {
  items: [
    { id: 'v1', name: 'BCG' },
    { id: 'v2', name: 'Hepatite B' },
  ],
  page: 1,
  pageSize: 2,
  totalCount: 2,
  totalPages: 1,
};

/** Cartão da pessoa com as aplicações informadas para a vacina v1. */
function cardWith(doses: VaccinationCard['vaccines'][number]['doses']): VaccinationCard {
  return {
    personId: 'p1',
    personName: 'Maria Silva',
    document: '12345678901',
    vaccines: doses.length
      ? [{ vaccineId: 'v1', vaccineName: 'BCG', totalDoses: doses.length, doses }]
      : [],
  };
}

/** Abre o autocomplete e escolhe a vacina pelo nome. */
async function chooseVaccine(name: string) {
  await userEvent.click(screen.getByRole('combobox', { name: 'Vacina' }));
  await userEvent.click(await screen.findByRole('option', { name }));
}

describe('VaccinationDialog', () => {
  it('sugere a dose 1 para vacina sem aplicacao', async () => {
    server.use(http.get('*/api/vaccines', () => HttpResponse.json(catalog)));

    renderWithProviders(
      <VaccinationDialog personId="p1" card={cardWith([])} open onClose={vi.fn()} />,
    );

    await chooseVaccine('BCG');

    expect(screen.getByLabelText(/número da dose/i)).toHaveValue(1);
  });

  it('sugere a proxima da sequencia do tipo escolhido', async () => {
    // Duas doses normais: a próxima normal é a 3, mas o próximo reforço é o 1.
    server.use(http.get('*/api/vaccines', () => HttpResponse.json(catalog)));

    const card = cardWith([
      { recordId: 'r1', vaccinationType: 'Dose', doseNumber: 1, vaccinationDate: '2024-01-10' },
      { recordId: 'r2', vaccinationType: 'Dose', doseNumber: 2, vaccinationDate: '2024-02-20' },
    ]);

    renderWithProviders(<VaccinationDialog personId="p1" card={card} open onClose={vi.fn()} />);

    await chooseVaccine('BCG');
    expect(screen.getByLabelText(/número da dose/i)).toHaveValue(3);

    await userEvent.click(screen.getByLabelText('Tipo'));
    await userEvent.click(await screen.findByRole('option', { name: 'Dose de reforço' }));

    expect(screen.getByLabelText(/número da dose/i)).toHaveValue(1);
  });

  it('desabilita reforco enquanto nao ha dose normal', async () => {
    server.use(http.get('*/api/vaccines', () => HttpResponse.json(catalog)));

    renderWithProviders(
      <VaccinationDialog personId="p1" card={cardWith([])} open onClose={vi.fn()} />,
    );

    await chooseVaccine('BCG');
    await userEvent.click(screen.getByLabelText('Tipo'));

    expect(await screen.findByRole('option', { name: 'Dose de reforço' })).toHaveAttribute(
      'aria-disabled',
      'true',
    );
  });

  it('limita a data entre a aplicacao anterior e hoje', async () => {
    server.use(http.get('*/api/vaccines', () => HttpResponse.json(catalog)));

    const card = cardWith([
      { recordId: 'r1', vaccinationType: 'Dose', doseNumber: 1, vaccinationDate: '2024-01-10' },
    ]);

    renderWithProviders(<VaccinationDialog personId="p1" card={card} open onClose={vi.fn()} />);

    await chooseVaccine('BCG');

    const campo = screen.getByLabelText(/data de aplicação/i);
    expect(campo).toHaveAttribute('min', '2024-01-10');
    expect(campo).toHaveAttribute('max');
  });

  it('envia o registro e fecha', async () => {
    let sent: unknown = null;

    server.use(
      http.get('*/api/vaccines', () => HttpResponse.json(catalog)),
      http.post('*/api/people/p1/vaccinations', async ({ request }) => {
        sent = await request.json();
        return HttpResponse.json({ id: 'novo' }, { status: 201 });
      }),
    );

    const onClose = vi.fn();

    renderWithProviders(
      <VaccinationDialog personId="p1" card={cardWith([])} open onClose={onClose} />,
    );

    await chooseVaccine('BCG');
    await userEvent.clear(screen.getByLabelText(/data de aplicação/i));
    await userEvent.type(screen.getByLabelText(/data de aplicação/i), '2024-03-15');
    await userEvent.click(screen.getByRole('button', { name: /registrar/i }));

    await waitFor(() => {
      expect(sent).toEqual({
        vaccineId: 'v1',
        vaccinationType: 'Dose',
        doseNumber: 1,
        vaccinationDate: '2024-03-15',
      });
    });
    expect(onClose).toHaveBeenCalledOnce();
  });

  it('mostra a regra violada que a API recusou, sem fechar', async () => {
    // RN06: a tela sugere, mas quem decide é o handler — e a mensagem já vem pronta.
    server.use(
      http.get('*/api/vaccines', () => HttpResponse.json(catalog)),
      http.post('*/api/people/p1/vaccinations', () =>
        HttpResponse.json(
          {
            status: 409,
            detail: "A dose 1 da vacina 'BCG' precisa ser registrada antes da dose 2.",
          },
          { status: 409 },
        ),
      ),
    );

    const onClose = vi.fn();

    renderWithProviders(
      <VaccinationDialog personId="p1" card={cardWith([])} open onClose={onClose} />,
    );

    await chooseVaccine('BCG');
    await userEvent.click(screen.getByRole('button', { name: /registrar/i }));

    expect(
      await screen.findByText("A dose 1 da vacina 'BCG' precisa ser registrada antes da dose 2."),
    ).toBeInTheDocument();
    expect(onClose).not.toHaveBeenCalled();
  });

  it('leva o erro de validacao ao campo, apesar do PascalCase', async () => {
    server.use(
      http.get('*/api/vaccines', () => HttpResponse.json(catalog)),
      http.post('*/api/people/p1/vaccinations', () =>
        HttpResponse.json(
          { status: 400, errors: { DoseNumber: ["'Número da dose' deve ser maior que 0."] } },
          { status: 400 },
        ),
      ),
    );

    renderWithProviders(
      <VaccinationDialog personId="p1" card={cardWith([])} open onClose={vi.fn()} />,
    );

    await chooseVaccine('BCG');
    await userEvent.click(screen.getByRole('button', { name: /registrar/i }));

    expect(await screen.findByLabelText(/número da dose/i)).toHaveAccessibleDescription(
      "'Número da dose' deve ser maior que 0.",
    );
  });

  it('busca a vacina no servidor, sem carregar o catalogo inteiro', async () => {
    // Com o catálogo grande, um select com tudo dentro seria impraticável: o trecho
    // digitado vai como `search` e a API devolve só o recorte.
    const searches: (string | null)[] = [];

    server.use(
      http.get('*/api/vaccines', ({ request }) => {
        const params = new URL(request.url).searchParams;
        searches.push(params.get('search'));

        return HttpResponse.json({ ...catalog, pageSize: Number(params.get('pageSize')) });
      }),
    );

    renderWithProviders(
      <VaccinationDialog personId="p1" card={cardWith([])} open onClose={vi.fn()} />,
    );

    await userEvent.type(screen.getByRole('combobox', { name: 'Vacina' }), 'hepat');

    await waitFor(() => {
      expect(searches).toContain('hepat');
    });
  });

  it('nao deixa registrar sem escolher a vacina', () => {
    server.use(http.get('*/api/vaccines', () => HttpResponse.json(catalog)));

    renderWithProviders(
      <VaccinationDialog personId="p1" card={cardWith([])} open onClose={vi.fn()} />,
    );

    expect(screen.getByRole('button', { name: /registrar/i })).toBeDisabled();
  });

  it('lista as vacinas do catalog', async () => {
    server.use(http.get('*/api/vaccines', () => HttpResponse.json(catalog)));

    renderWithProviders(
      <VaccinationDialog personId="p1" card={cardWith([])} open onClose={vi.fn()} />,
    );

    await userEvent.click(await screen.findByLabelText('Vacina'));

    const options = within(await screen.findByRole('listbox')).getAllByRole('option');
    expect(options.map(option => option.textContent)).toEqual(['BCG', 'Hepatite B']);
  });
});
