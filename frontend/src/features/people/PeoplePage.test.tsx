import { describe, expect, it } from 'vitest';
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';

import { server } from '../../test/server';
import { renderWithProviders } from '../../test/renderWithProviders';
import PeoplePage from './PeoplePage';

const rota = '*/api/people';

function pagina(
  pessoas: { name: string; document: string }[],
  totalCount = pessoas.length,
  pageSize = 20,
) {
  return {
    items: pessoas.map((pessoa, indice) => ({ id: `p${String(indice)}`, ...pessoa })),
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
    server.use(http.get(rota, () => HttpResponse.json(pagina([maria, joao]))));

    renderWithProviders(<PeoplePage />);

    expect(await screen.findByText('Maria Silva')).toBeInTheDocument();
    expect(screen.getByText('123.456.789-01')).toBeInTheDocument();
    expect(screen.getByText('João Souza')).toBeInTheDocument();
  });

  it('convida ao cadastro quando nao ha ninguem', async () => {
    server.use(http.get(rota, () => HttpResponse.json(pagina([]))));

    renderWithProviders(<PeoplePage />);

    expect(await screen.findByText(/nenhuma pessoa cadastrada ainda/i)).toBeInTheDocument();
  });

  it('mostra o erro da API e esconde a tabela', async () => {
    server.use(
      http.get(rota, () =>
        HttpResponse.json({ status: 500, detail: 'Falha ao consultar.' }, { status: 500 }),
      ),
    );

    renderWithProviders(<PeoplePage />);

    expect(await screen.findByText('Falha ao consultar.')).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });

  it('busca por nome pelo servidor', async () => {
    const buscas: (string | null)[] = [];

    server.use(
      http.get(rota, ({ request }) => {
        buscas.push(new URL(request.url).searchParams.get('search'));
        return HttpResponse.json(pagina([maria]));
      }),
    );

    renderWithProviders(<PeoplePage />);
    await screen.findByText('Maria Silva');

    await userEvent.type(screen.getByLabelText(/buscar por nome ou cpf/i), 'Maria');

    await waitFor(() => {
      expect(buscas).toContain('Maria');
    });
  });

  it('tira a mascara do CPF digitado na busca', async () => {
    // O CPF está gravado sem pontuação: "123.456" precisa chegar como "123456".
    const buscas: (string | null)[] = [];

    server.use(
      http.get(rota, ({ request }) => {
        buscas.push(new URL(request.url).searchParams.get('search'));
        return HttpResponse.json(pagina([maria]));
      }),
    );

    renderWithProviders(<PeoplePage />);
    await screen.findByText('Maria Silva');

    await userEvent.type(screen.getByLabelText(/buscar por nome ou cpf/i), '123.456');

    await waitFor(() => {
      expect(buscas).toContain('123456');
    });
  });

  it('pagina pelo servidor, convertendo o indice da tabela', async () => {
    const paginasPedidas: (string | null)[] = [];

    server.use(
      http.get(rota, ({ request }) => {
        const page = new URL(request.url).searchParams.get('page');
        paginasPedidas.push(page);

        return HttpResponse.json({
          ...pagina(page === '2' ? [joao] : [maria], 25),
          page: Number(page),
        });
      }),
    );

    renderWithProviders(<PeoplePage />);
    await screen.findByText('Maria Silva');

    await userEvent.click(screen.getByRole('button', { name: /próxima página/i }));

    expect(await screen.findByText('João Souza')).toBeInTheDocument();
    expect(paginasPedidas).toEqual(['1', '2']);
  });

  it('cadastra uma pessoa e recarrega a listagem', async () => {
    let cadastradas = [maria];

    server.use(
      http.get(rota, () => HttpResponse.json(pagina(cadastradas))),
      http.post(rota, async ({ request }) => {
        const body = (await request.json()) as { name: string; document: string };
        cadastradas = [...cadastradas, body];

        return HttpResponse.json({ id: 'nova', ...body }, { status: 201 });
      }),
    );

    renderWithProviders(<PeoplePage />);
    await screen.findByText('Maria Silva');

    await userEvent.click(screen.getByRole('button', { name: /nova pessoa/i }));

    const dialogo = screen.getByRole('dialog');
    await userEvent.type(within(dialogo).getByLabelText(/nome/i), 'João Souza');
    await userEvent.type(within(dialogo).getByLabelText(/cpf/i), '98765432100');
    await userEvent.click(within(dialogo).getByRole('button', { name: /salvar/i }));

    expect(await screen.findByText('João Souza')).toBeInTheDocument();
  });

  it('exclui pela linha e tira a pessoa da lista', async () => {
    let cadastradas = [maria, joao];

    server.use(
      http.get(rota, () => HttpResponse.json(pagina(cadastradas))),
      http.get('*/api/people/p0/vaccination-card', () =>
        HttpResponse.json({
          personId: 'p0',
          personName: maria.name,
          document: maria.document,
          vaccines: [{ vaccineId: 'v1', vaccineName: 'BCG', totalDoses: 2, doses: [] }],
        }),
      ),
      http.delete('*/api/people/p0', () => {
        cadastradas = [joao];
        return new HttpResponse(null, { status: 204 });
      }),
    );

    renderWithProviders(<PeoplePage />);
    await screen.findByText('Maria Silva');

    await userEvent.click(screen.getByRole('button', { name: 'Excluir Maria Silva' }));

    // O aviso de cascata precisa aparecer antes de a exclusão ser possível.
    expect(await screen.findByText('2 registros de vacinação serão perdidos.')).toBeInTheDocument();

    const dialogo = screen.getByRole('dialog');
    await userEvent.click(within(dialogo).getByRole('button', { name: /excluir/i }));

    await waitFor(() => {
      expect(screen.queryByText('Maria Silva')).not.toBeInTheDocument();
    });
    expect(screen.getByText('João Souza')).toBeInTheDocument();
  });
});
