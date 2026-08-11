import { describe, expect, it } from 'vitest';
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { Route, Routes } from 'react-router-dom';

import { server } from '../../test/server';
import { renderWithProviders } from '../../test/renderWithProviders';
import type { VaccinationCard } from '../../api/types';
import VaccinationCardPage from './VaccinationCardPage';

const cardRoute = '*/api/people/p1/vaccination-card';

const fullCard: VaccinationCard = {
  personId: 'p1',
  personName: 'Maria Silva',
  document: '12345678901',
  vaccines: [
    {
      vaccineId: 'v1',
      vaccineName: 'BCG',
      totalDoses: 2,
      doses: [
        { recordId: 'r1', vaccinationType: 'Dose', doseNumber: 1, vaccinationDate: '2024-01-10' },
        {
          recordId: 'r2',
          vaccinationType: 'BoosterDose',
          doseNumber: 1,
          vaccinationDate: '2024-06-20',
        },
      ],
    },
  ],
};

/** A página lê o personId da rota, então precisa ser montada dentro dela. */
function renderCard() {
  return renderWithProviders(
    <Routes>
      <Route path="/pessoas/:personId/cartao" element={<VaccinationCardPage />} />
      <Route path="/pessoas" element={<p>Lista de pessoas</p>} />
    </Routes>,
    '/pessoas/p1/cartao',
  );
}

describe('VaccinationCardPage', () => {
  it('mostra a pessoa e as aplicacoes agrupadas por vacina', async () => {
    server.use(http.get(cardRoute, () => HttpResponse.json(fullCard)));

    renderCard();

    expect(await screen.findByText('Maria Silva')).toBeInTheDocument();
    expect(screen.getByText('CPF 123.456.789-01')).toBeInTheDocument();
    expect(screen.getByText('BCG')).toBeInTheDocument();
    expect(screen.getByText('2 aplicações')).toBeInTheDocument();
  });

  it('exibe a data no formato brasileiro e o rotulo do tipo', async () => {
    server.use(http.get(cardRoute, () => HttpResponse.json(fullCard)));

    renderCard();

    expect(await screen.findByText('10/01/2024')).toBeInTheDocument();
    expect(screen.getByText('20/06/2024')).toBeInTheDocument();
    expect(screen.getByText('Dose de reforço')).toBeInTheDocument();
  });

  it('oferece a volta para a lista de pessoas', async () => {
    // O cartão é alcançado por dentro da lista, e a barra superior não tem link para ele.
    server.use(http.get(cardRoute, () => HttpResponse.json(fullCard)));

    renderCard();
    await screen.findByText('Maria Silva');

    const backLink = screen.getByRole('link', { name: /voltar para pessoas/i });

    expect(backLink).toHaveAttribute('href', '/pessoas');

    await userEvent.click(backLink);
    expect(await screen.findByText('Lista de pessoas')).toBeInTheDocument();
  });

  it('convida ao registro quando o cartao esta vazio', async () => {
    server.use(
      http.get(cardRoute, () => HttpResponse.json({ ...fullCard, vaccines: [] })),
    );

    renderCard();

    expect(await screen.findByText(/nenhuma aplicação registrada ainda/i)).toBeInTheDocument();
  });

  it('oferece a volta para pessoas quando a pessoa nao existe', async () => {
    server.use(
      http.get(cardRoute, () =>
        HttpResponse.json({ status: 404, detail: 'Pessoa não encontrada.' }, { status: 404 }),
      ),
    );

    renderCard();

    expect(await screen.findByText('Pessoa não encontrada.')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /voltar para pessoas/i })).toBeInTheDocument();
  });

  it('remove uma aplicacao apos a confirmacao', async () => {
    let doses = fullCard.vaccines[0].doses;

    server.use(
      http.get(cardRoute, () =>
        HttpResponse.json({
          ...fullCard,
          vaccines: [{ ...fullCard.vaccines[0], totalDoses: doses.length, doses }],
        }),
      ),
      http.delete('*/api/people/p1/vaccinations/r2', () => {
        doses = doses.filter(dose => dose.recordId !== 'r2');
        return new HttpResponse(null, { status: 204 });
      }),
    );

    renderCard();
    await screen.findByText('20/06/2024');

    await userEvent.click(
      screen.getByRole('button', { name: 'Remover Dose de reforço 1 de BCG' }),
    );

    const dialog = await screen.findByRole('dialog');
    expect(within(dialog).getByText(/não pode ser desfeita/i)).toBeInTheDocument();

    await userEvent.click(within(dialog).getByRole('button', { name: /remover/i }));

    await waitFor(() => {
      expect(screen.queryByText('20/06/2024')).not.toBeInTheDocument();
    });
    expect(screen.getByText('10/01/2024')).toBeInTheDocument();
  });

  it('mostra o erro da remocao e mantem o dialog aberto', async () => {
    server.use(
      http.get(cardRoute, () => HttpResponse.json(fullCard)),
      http.delete('*/api/people/p1/vaccinations/r2', () =>
        HttpResponse.json({ status: 404, detail: 'Registro não encontrado.' }, { status: 404 }),
      ),
    );

    renderCard();
    await screen.findByText('20/06/2024');

    await userEvent.click(
      screen.getByRole('button', { name: 'Remover Dose de reforço 1 de BCG' }),
    );
    const dialog = await screen.findByRole('dialog');
    await userEvent.click(within(dialog).getByRole('button', { name: /remover/i }));

    expect(await screen.findByText('Registro não encontrado.')).toBeInTheDocument();
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('registra uma aplicacao e recarrega o cartao', async () => {
    let doses = [...fullCard.vaccines[0].doses];

    server.use(
      http.get('*/api/vaccines', () =>
        HttpResponse.json({
          items: [{ id: 'v1', name: 'BCG' }],
          page: 1,
          pageSize: 1,
          totalCount: 1,
          totalPages: 1,
        }),
      ),
      http.get(cardRoute, () =>
        HttpResponse.json({
          ...fullCard,
          vaccines: [{ ...fullCard.vaccines[0], totalDoses: doses.length, doses }],
        }),
      ),
      http.post('*/api/people/p1/vaccinations', () => {
        doses = [
          ...doses,
          {
            recordId: 'r3',
            vaccinationType: 'Dose',
            doseNumber: 2,
            vaccinationDate: '2024-08-01',
          },
        ];
        return HttpResponse.json({ id: 'r3' }, { status: 201 });
      }),
    );

    renderCard();
    await screen.findByText('10/01/2024');

    await userEvent.click(screen.getByRole('button', { name: /registrar aplicação/i }));

    const dialog = await screen.findByRole('dialog');
    await userEvent.click(within(dialog).getByLabelText('Vacina'));
    await userEvent.click(await screen.findByRole('option', { name: 'BCG' }));
    await userEvent.click(within(dialog).getByRole('button', { name: /^registrar$/i }));

    expect(await screen.findByText('01/08/2024')).toBeInTheDocument();
  });
});
