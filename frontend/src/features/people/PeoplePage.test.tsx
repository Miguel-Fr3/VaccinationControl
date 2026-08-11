import { describe, expect, it } from 'vitest';
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';

import { server } from '../../test/server';
import { renderWithProviders } from '../../test/renderWithProviders';
import PeoplePage from './PeoplePage';

const route = '*/api/people';

function pagedResult(
  people: { name: string; document: string }[],
  totalCount = people.length,
  pageSize = 20,
) {
  return {
    items: people.map((person, index) => ({ id: `p${String(index)}`, ...person })),
    page: 1,
    pageSize,
    totalCount,
    totalPages: Math.ceil(totalCount / pageSize),
  };
}

const maria = { name: 'Maria Silva', document: '12345678901' };
const joao = { name: 'João Souza', document: '98765432100' };

describe('PeoplePage', () => {
  it('lista nome e CPF mascarado', async () => {
    // A API devolve os 11 dígitos crus; a máscara é aplicada na exibição.
    server.use(http.get(route, () => HttpResponse.json(pagedResult([maria, joao]))));

    renderWithProviders(<PeoplePage />);

    expect(await screen.findByText('Maria Silva')).toBeInTheDocument();
    expect(screen.getByText('123.456.789-01')).toBeInTheDocument();
    expect(screen.getByText('João Souza')).toBeInTheDocument();
  });

  it('convida ao cadastro quando nao ha ninguem', async () => {
    server.use(http.get(route, () => HttpResponse.json(pagedResult([]))));

    renderWithProviders(<PeoplePage />);

    expect(await screen.findByText(/nenhuma pessoa cadastrada ainda/i)).toBeInTheDocument();
  });

  it('mostra o erro da API e esconde a tabela', async () => {
    server.use(
      http.get(route, () =>
        HttpResponse.json({ status: 500, detail: 'Falha ao consultar.' }, { status: 500 }),
      ),
    );

    renderWithProviders(<PeoplePage />);

    expect(await screen.findByText('Falha ao consultar.')).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });

  it('busca por nome pelo servidor', async () => {
    const searches: (string | null)[] = [];

    server.use(
      http.get(route, ({ request }) => {
        searches.push(new URL(request.url).searchParams.get('search'));
        return HttpResponse.json(pagedResult([maria]));
      }),
    );

    renderWithProviders(<PeoplePage />);
    await screen.findByText('Maria Silva');

    await userEvent.type(screen.getByLabelText(/buscar por nome ou cpf/i), 'Maria');

    await waitFor(() => {
      expect(searches).toContain('Maria');
    });
  });

  it('tira a mascara do CPF digitado na busca', async () => {
    // O CPF está gravado sem pontuação: "123.456" precisa chegar como "123456".
    const searches: (string | null)[] = [];

    server.use(
      http.get(route, ({ request }) => {
        searches.push(new URL(request.url).searchParams.get('search'));
        return HttpResponse.json(pagedResult([maria]));
      }),
    );

    renderWithProviders(<PeoplePage />);
    await screen.findByText('Maria Silva');

    await userEvent.type(screen.getByLabelText(/buscar por nome ou cpf/i), '123.456');

    await waitFor(() => {
      expect(searches).toContain('123456');
    });
  });

  it('pagina pelo servidor, convertendo o indice da tabela', async () => {
    const requestedPages: (string | null)[] = [];

    server.use(
      http.get(route, ({ request }) => {
        const page = new URL(request.url).searchParams.get('page');
        requestedPages.push(page);

        return HttpResponse.json({
          ...pagedResult(page === '2' ? [joao] : [maria], 25),
          page: Number(page),
        });
      }),
    );

    renderWithProviders(<PeoplePage />);
    await screen.findByText('Maria Silva');

    await userEvent.click(screen.getByRole('button', { name: /próxima página/i }));

    expect(await screen.findByText('João Souza')).toBeInTheDocument();
    expect(requestedPages).toEqual(['1', '2']);
  });

  it('cadastra uma pessoa e recarrega a listagem', async () => {
    let registered = [maria];

    server.use(
      http.get(route, () => HttpResponse.json(pagedResult(registered))),
      http.post(route, async ({ request }) => {
        const body = (await request.json()) as { name: string; document: string };
        registered = [...registered, body];

        return HttpResponse.json({ id: 'nova', ...body }, { status: 201 });
      }),
    );

    renderWithProviders(<PeoplePage />);
    await screen.findByText('Maria Silva');

    await userEvent.click(screen.getByRole('button', { name: /nova pessoa/i }));

    const dialog = screen.getByRole('dialog');
    await userEvent.type(within(dialog).getByLabelText(/nome/i), 'João Souza');
    await userEvent.type(within(dialog).getByLabelText(/cpf/i), '98765432100');
    await userEvent.click(within(dialog).getByRole('button', { name: /salvar/i }));

    expect(await screen.findByText('João Souza')).toBeInTheDocument();
  });

  it('exclui pela linha e tira a pessoa da lista', async () => {
    let registered = [maria, joao];

    server.use(
      http.get(route, () => HttpResponse.json(pagedResult(registered))),
      http.get('*/api/people/p0/vaccination-card', () =>
        HttpResponse.json({
          personId: 'p0',
          personName: maria.name,
          document: maria.document,
          vaccines: [{ vaccineId: 'v1', vaccineName: 'BCG', totalDoses: 2, doses: [] }],
        }),
      ),
      http.delete('*/api/people/p0', () => {
        registered = [joao];
        return new HttpResponse(null, { status: 204 });
      }),
    );

    renderWithProviders(<PeoplePage />);
    await screen.findByText('Maria Silva');

    await userEvent.click(screen.getByRole('button', { name: 'Excluir Maria Silva' }));

    // O aviso de cascata precisa aparecer antes de a exclusão ser possível.
    expect(await screen.findByText('2 registros de vacinação serão perdidos.')).toBeInTheDocument();

    const dialog = screen.getByRole('dialog');
    await userEvent.click(within(dialog).getByRole('button', { name: /excluir/i }));

    await waitFor(() => {
      expect(screen.queryByText('Maria Silva')).not.toBeInTheDocument();
    });
    expect(screen.getByText('João Souza')).toBeInTheDocument();
  });
});
