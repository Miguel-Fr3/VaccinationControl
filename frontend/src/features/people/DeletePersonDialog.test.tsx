import { describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';

import { server } from '../../test/server';
import { renderWithProviders } from '../../test/renderWithProviders';
import type { Person } from '../../api/types';
import { DeletePersonDialog } from './DeletePersonDialog';

const person: Person = { id: 'p1', name: 'Maria Silva', document: '12345678901' };

const cardRoute = '*/api/people/p1/vaccination-card';
const personRoute = '*/api/people/p1';

/** Cartão com o total de aplicações distribuído entre duas vacinas. */
function card(dosesPerVaccine: number[]) {
  return {
    personId: person.id,
    personName: person.name,
    document: person.document,
    vaccines: dosesPerVaccine.map((totalDoses, index) => ({
      vaccineId: `v${String(index)}`,
      vaccineName: `Vacina ${String(index)}`,
      totalDoses,
      doses: [],
    })),
  };
}

describe('DeletePersonDialog', () => {
  it('avisa que o cartao vai junto', async () => {
    server.use(http.get(cardRoute, () => HttpResponse.json(card([]))));

    renderWithProviders(<DeletePersonDialog person={person} onClose={vi.fn()} />);

    expect(screen.getByText(/apaga também o cartão de vacinação/i)).toBeInTheDocument();
    expect(screen.getByText(/não pode ser desfeita/i)).toBeInTheDocument();

    // Espera a consulta do cartão para o teste não terminar com requisição em voo.
    await screen.findByText(/nenhum registro de vacinação/i);
  });

  it('informa quantos registros serao perdidos', async () => {
    // 3 + 2 aplicações em duas vacinas: o aviso soma o cartão inteiro.
    server.use(http.get(cardRoute, () => HttpResponse.json(card([3, 2]))));

    renderWithProviders(<DeletePersonDialog person={person} onClose={vi.fn()} />);

    expect(await screen.findByText('5 registros de vacinação serão perdidos.')).toBeInTheDocument();
  });

  it('usa o singular quando ha um registro so', async () => {
    server.use(http.get(cardRoute, () => HttpResponse.json(card([1]))));

    renderWithProviders(<DeletePersonDialog person={person} onClose={vi.fn()} />);

    expect(await screen.findByText('1 registro de vacinação será perdido.')).toBeInTheDocument();
  });

  it('diz quando nao ha registro nenhum', async () => {
    server.use(http.get(cardRoute, () => HttpResponse.json(card([]))));

    renderWithProviders(<DeletePersonDialog person={person} onClose={vi.fn()} />);

    expect(
      await screen.findByText('Esta pessoa não tem nenhum registro de vacinação.'),
    ).toBeInTheDocument();
  });

  it('nao deixa confirmar antes de saber o tamanho do estrago', async () => {
    server.use(http.get(cardRoute, () => HttpResponse.json(card([2]))));

    renderWithProviders(<DeletePersonDialog person={person} onClose={vi.fn()} />);

    expect(screen.getByRole('button', { name: /excluir/i })).toBeDisabled();

    await screen.findByText('2 registros de vacinação serão perdidos.');
    expect(screen.getByRole('button', { name: /excluir/i })).toBeEnabled();
  });

  it('mantem o aviso de cascata quando o cartao nao pode ser consultado', async () => {
    // Sem o número o aviso continua valendo — e a exclusão não pode ficar bloqueada por isso.
    server.use(http.get(cardRoute, () => HttpResponse.json({ status: 500 }, { status: 500 })));

    renderWithProviders(<DeletePersonDialog person={person} onClose={vi.fn()} />);

    expect(await screen.findByText(/não foi possível verificar o cartão/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /excluir/i })).toBeEnabled();
  });

  it('exclui e fecha ao confirmar', async () => {
    const onClose = vi.fn();
    let deleted = false;

    server.use(
      http.get(cardRoute, () => HttpResponse.json(card([1]))),
      http.delete(personRoute, () => {
        deleted = true;
        return new HttpResponse(null, { status: 204 });
      }),
    );

    renderWithProviders(<DeletePersonDialog person={person} onClose={onClose} />);
    await screen.findByText('1 registro de vacinação será perdido.');

    await userEvent.click(screen.getByRole('button', { name: /excluir/i }));

    await waitFor(() => {
      expect(onClose).toHaveBeenCalledOnce();
    });
    expect(deleted).toBe(true);
  });

  it('mostra o erro da API e mantem o dialogo aberto', async () => {
    const onClose = vi.fn();

    server.use(
      http.get(cardRoute, () => HttpResponse.json(card([1]))),
      http.delete(personRoute, () =>
        HttpResponse.json(
          { status: 404, detail: 'Pessoa não encontrada.' },
          { status: 404 },
        ),
      ),
    );

    renderWithProviders(<DeletePersonDialog person={person} onClose={onClose} />);
    await screen.findByText('1 registro de vacinação será perdido.');

    await userEvent.click(screen.getByRole('button', { name: /excluir/i }));

    expect(await screen.findByText('Pessoa não encontrada.')).toBeInTheDocument();
    expect(onClose).not.toHaveBeenCalled();
  });
});
