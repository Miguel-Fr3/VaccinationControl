import { describe, expect, it } from 'vitest';
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';

import { server } from '../../test/server';
import { renderWithProviders } from '../../test/renderWithProviders';
import VaccinesPage from './VaccinesPage';

const rota = '*/api/vaccines';

/** Envelope da API com o mesmo formato do `PagedResult<T>`. */
function pagina(nomes: string[], totalCount = nomes.length, pageSize = 20) {
  return {
    items: nomes.map((name, indice) => ({ id: `id-${indice}-${name}`, name })),
    page: 1,
    pageSize,
    totalCount,
    totalPages: Math.ceil(totalCount / pageSize),
  };
}

describe('VaccinesPage', () => {
  it('mostra o carregamento antes da resposta', async () => {
    server.use(http.get(rota, () => HttpResponse.json(pagina(['BCG']))));

    renderWithProviders(<VaccinesPage />);

    expect(screen.getByRole('progressbar')).toBeInTheDocument();

    // Espera a listagem para o teste não terminar com requisição em voo.
    expect(await screen.findByText('BCG')).toBeInTheDocument();
  });

  it('lista as vacinas devolvidas pela API', async () => {
    server.use(http.get(rota, () => HttpResponse.json(pagina(['BCG', 'Hepatite B']))));

    renderWithProviders(<VaccinesPage />);

    expect(await screen.findByText('BCG')).toBeInTheDocument();
    expect(screen.getByText('Hepatite B')).toBeInTheDocument();
  });

  it('convida ao cadastro quando nao ha nenhuma vacina', async () => {
    server.use(http.get(rota, () => HttpResponse.json(pagina([]))));

    renderWithProviders(<VaccinesPage />);

    expect(await screen.findByText(/nenhuma vacina cadastrada ainda/i)).toBeInTheDocument();
  });

  it('mostra a mensagem de erro da API e esconde a tabela', async () => {
    // O detail do ProblemDetails precisa chegar à tela: é a mensagem escrita para o usuário.
    server.use(
      http.get(rota, () =>
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
    let chamadas = 0;

    server.use(
      http.get(rota, () => {
        chamadas += 1;

        return chamadas === 1
          ? HttpResponse.json(pagina(['BCG']))
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
    const buscas: (string | null)[] = [];

    server.use(
      http.get(rota, ({ request }) => {
        buscas.push(new URL(request.url).searchParams.get('search'));

        return HttpResponse.json(pagina(['Hepatite B']));
      }),
    );

    renderWithProviders(<VaccinesPage />);
    await screen.findByText('Hepatite B');

    await userEvent.type(screen.getByLabelText(/buscar por nome/i), 'hepat');

    // O debounce agrupa as teclas: a busca vai numa requisição só, e não em cinco.
    await waitFor(() => {
      expect(buscas).toContain('hepat');
    });
  });

  it('avisa quando a busca nao encontra nada', async () => {
    server.use(
      http.get(rota, ({ request }) => {
        const search = new URL(request.url).searchParams.get('search');

        return HttpResponse.json(search ? pagina([]) : pagina(['BCG']));
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
    const paginasPedidas: (string | null)[] = [];

    server.use(
      http.get(rota, ({ request }) => {
        const page = new URL(request.url).searchParams.get('page');
        paginasPedidas.push(page);

        // O total precisa passar do pageSize de 20, senão a tabela tem uma página só e o
        // botão de avançar nasce desabilitado.
        return HttpResponse.json({
          ...pagina(page === '2' ? ['Triplice Viral'] : ['BCG', 'Hepatite B'], 25),
          page: Number(page),
        });
      }),
    );

    renderWithProviders(<VaccinesPage />);
    await screen.findByText('BCG');

    await userEvent.click(screen.getByRole('button', { name: /próxima página/i }));

    expect(await screen.findByText('Triplice Viral')).toBeInTheDocument();
    expect(paginasPedidas).toEqual(['1', '2']);
  });

  it('volta para a primeira pagina ao filtrar', async () => {
    // Sem isso, quem está na página 2 e digita uma busca pede a página 2 do resultado novo,
    // que costuma não existir — e a listagem vem vazia sem explicação.
    const pedidos: { page: string | null; search: string | null }[] = [];

    server.use(
      http.get(rota, ({ request }) => {
        const parametros = new URL(request.url).searchParams;
        pedidos.push({ page: parametros.get('page'), search: parametros.get('search') });

        return HttpResponse.json({ ...pagina(['BCG'], 25), page: Number(parametros.get('page')) });
      }),
    );

    renderWithProviders(<VaccinesPage />);
    await screen.findByText('BCG');

    await userEvent.click(screen.getByRole('button', { name: /próxima página/i }));
    await waitFor(() => {
      expect(pedidos.at(-1)?.page).toBe('2');
    });

    await userEvent.type(screen.getByLabelText(/buscar por nome/i), 'bcg');

    await waitFor(() => {
      expect(pedidos.at(-1)).toEqual({ page: '1', search: 'bcg' });
    });
  });

  it('cadastra uma vacina e recarrega a listagem', async () => {
    let cadastradas: string[] = ['BCG'];

    server.use(
      http.get(rota, () => HttpResponse.json(pagina(cadastradas))),
      http.post(rota, async ({ request }) => {
        const body = (await request.json()) as { name: string };
        cadastradas = [...cadastradas, body.name];

        return HttpResponse.json({ id: 'nova', name: body.name }, { status: 201 });
      }),
    );

    renderWithProviders(<VaccinesPage />);
    await screen.findByText('BCG');

    await userEvent.click(screen.getByRole('button', { name: /nova vacina/i }));

    const dialogo = screen.getByRole('dialog');
    await userEvent.type(within(dialogo).getByLabelText(/nome/i), 'Febre Amarela');
    await userEvent.click(within(dialogo).getByRole('button', { name: /salvar/i }));

    expect(await screen.findByText('Febre Amarela')).toBeInTheDocument();

    // O diálogo do MUI só sai do DOM ao fim da transição de fechamento.
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
  });
});
