import { describe, expect, it } from 'vitest';
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';

import { server } from '../../test/server';
import { renderWithProviders } from '../../test/renderWithProviders';
import VaccinesPage from './VaccinesPage';

const route = '*/api/vaccines';

/** Envelope da API com o mesmo formato do `PagedResult<T>`. */
function pagedResult(names: string[], totalCount = names.length, pageSize = 20) {
  return {
    items: names.map((name, index) => ({ id: `id-${index}-${name}`, name })),
    page: 1,
    pageSize,
    totalCount,
    totalPages: Math.ceil(totalCount / pageSize),
  };
}

describe('VaccinesPage', () => {
  it('mostra o carregamento antes da resposta', async () => {
    server.use(http.get(route, () => HttpResponse.json(pagedResult(['BCG']))));

    renderWithProviders(<VaccinesPage />);

    expect(screen.getByRole('progressbar')).toBeInTheDocument();

    // Espera a listagem para o teste não terminar com requisição em voo.
    expect(await screen.findByText('BCG')).toBeInTheDocument();
  });

  it('lista as vacinas devolvidas pela API', async () => {
    server.use(http.get(route, () => HttpResponse.json(pagedResult(['BCG', 'Hepatite B']))));

    renderWithProviders(<VaccinesPage />);

    expect(await screen.findByText('BCG')).toBeInTheDocument();
    expect(screen.getByText('Hepatite B')).toBeInTheDocument();
  });

  it('convida ao cadastro quando nao ha nenhuma vacina', async () => {
    server.use(http.get(route, () => HttpResponse.json(pagedResult([]))));

    renderWithProviders(<VaccinesPage />);

    expect(await screen.findByText(/nenhuma vacina cadastrada ainda/i)).toBeInTheDocument();
  });

  it('mostra a mensagem de erro da API e esconde a tabela', async () => {
    // O detail do ProblemDetails precisa chegar à tela: é a mensagem escrita para o usuário.
    server.use(
      http.get(route, () =>
        HttpResponse.json(
          { title: 'Requisição inválida', status: 400, detail: 'Falha ao consultar o catálogo.' },
          { status: 400 },
        ),
      ),
    );

    renderWithProviders(<VaccinesPage />);

    expect(await screen.findByText('Falha ao consultar o catálogo.')).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });

  it('nao mistura a tabela antiga com o erro da busca seguinte', async () => {
    // Com keepPreviousData o data anterior sobrevive ao erro; a tabela exibida não
    // corresponderia ao filtro digitado.
    let calls = 0;

    server.use(
      http.get(route, () => {
        calls += 1;

        return calls === 1
          ? HttpResponse.json(pagedResult(['BCG']))
          : HttpResponse.json({ status: 500, detail: 'Servidor indisponivel.' }, { status: 500 });
      }),
    );

    renderWithProviders(<VaccinesPage />);
    expect(await screen.findByText('BCG')).toBeInTheDocument();

    await userEvent.type(screen.getByLabelText(/buscar por nome/i), 'hepat');

    expect(await screen.findByText('Servidor indisponivel.')).toBeInTheDocument();
    expect(screen.queryByText('BCG')).not.toBeInTheDocument();
  });

  it('envia o trecho buscado como parametro de consulta', async () => {
    const searches: (string | null)[] = [];

    server.use(
      http.get(route, ({ request }) => {
        searches.push(new URL(request.url).searchParams.get('search'));

        return HttpResponse.json(pagedResult(['Hepatite B']));
      }),
    );

    renderWithProviders(<VaccinesPage />);
    await screen.findByText('Hepatite B');

    await userEvent.type(screen.getByLabelText(/buscar por nome/i), 'hepat');

    // O debounce agrupa as teclas: a busca vai numa requisição só, e não em cinco.
    await waitFor(() => {
      expect(searches).toContain('hepat');
    });
  });

  it('avisa quando a busca nao encontra nada', async () => {
    server.use(
      http.get(route, ({ request }) => {
        const search = new URL(request.url).searchParams.get('search');

        return HttpResponse.json(search ? pagedResult([]) : pagedResult(['BCG']));
      }),
    );

    renderWithProviders(<VaccinesPage />);
    await screen.findByText('BCG');

    await userEvent.type(screen.getByLabelText(/buscar por nome/i), 'zzz');

    expect(await screen.findByText(/nenhuma vacina encontrada para "zzz"/i)).toBeInTheDocument();
  });

  it('pagina pelo servidor, convertendo o indice da tabela', async () => {
    // A API conta a partir de 1 e a tabela do MUI a partir de 0: o clique em "próxima"
    // precisa pedir page=2, não page=1.
    const requestedPages: (string | null)[] = [];

    server.use(
      http.get(route, ({ request }) => {
        const page = new URL(request.url).searchParams.get('page');
        requestedPages.push(page);

        // O total precisa passar do pageSize de 20, senão a tabela tem uma página só e o
        // botão de avançar nasce desabilitado.
        return HttpResponse.json({
          ...pagedResult(page === '2' ? ['Triplice Viral'] : ['BCG', 'Hepatite B'], 25),
          page: Number(page),
        });
      }),
    );

    renderWithProviders(<VaccinesPage />);
    await screen.findByText('BCG');

    await userEvent.click(screen.getByRole('button', { name: /próxima página/i }));

    expect(await screen.findByText('Triplice Viral')).toBeInTheDocument();
    expect(requestedPages).toEqual(['1', '2']);
  });

  it('volta para a primeira pagina ao filtrar', async () => {
    // Sem isso, quem está na página 2 e digita uma busca pede a página 2 do resultado novo,
    // que costuma não existir — e a listagem vem vazia sem explicação.
    const requests: { page: string | null; search: string | null }[] = [];

    server.use(
      http.get(route, ({ request }) => {
        const params = new URL(request.url).searchParams;
        requests.push({ page: params.get('page'), search: params.get('search') });

        return HttpResponse.json({ ...pagedResult(['BCG'], 25), page: Number(params.get('page')) });
      }),
    );

    renderWithProviders(<VaccinesPage />);
    await screen.findByText('BCG');

    await userEvent.click(screen.getByRole('button', { name: /próxima página/i }));
    await waitFor(() => {
      expect(requests.at(-1)?.page).toBe('2');
    });

    await userEvent.type(screen.getByLabelText(/buscar por nome/i), 'bcg');

    await waitFor(() => {
      expect(requests.at(-1)).toEqual({ page: '1', search: 'bcg' });
    });
  });

  it('cadastra uma vacina e recarrega a listagem', async () => {
    let registered: string[] = ['BCG'];

    server.use(
      http.get(route, () => HttpResponse.json(pagedResult(registered))),
      http.post(route, async ({ request }) => {
        const body = (await request.json()) as { name: string };
        registered = [...registered, body.name];

        return HttpResponse.json({ id: 'nova', name: body.name }, { status: 201 });
      }),
    );

    renderWithProviders(<VaccinesPage />);
    await screen.findByText('BCG');

    await userEvent.click(screen.getByRole('button', { name: /nova vacina/i }));

    const dialog = screen.getByRole('dialog');
    await userEvent.type(within(dialog).getByLabelText(/nome/i), 'Febre Amarela');
    await userEvent.click(within(dialog).getByRole('button', { name: /salvar/i }));

    expect(await screen.findByText('Febre Amarela')).toBeInTheDocument();

    // O diálogo do MUI só sai do DOM ao fim da transição de fechamento.
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
  });
});
