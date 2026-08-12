import { describe, expect, it } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';

import { server } from './test/server';
import { renderWithProviders } from './test/renderWithProviders';
import { SessionProvider } from './auth/SessionProvider';
import App from './App';

const emptyPage = { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 };

/** Sessão aberta e listagens vazias: o suficiente para cada route renderizar sua tela. */
function serveSession() {
  server.use(
    http.get('*/api/auth/me', () =>
      HttpResponse.json({ userId: 'u1', email: 'maria@teste.local' }),
    ),
    http.get('*/api/people', () => HttpResponse.json(emptyPage)),
    http.get('*/api/vaccines', () => HttpResponse.json(emptyPage)),
    http.get('*/api/people/p1/vaccination-card', () =>
      HttpResponse.json({
        personId: 'p1',
        personName: 'Maria Silva',
        document: '12345678901',
        vaccines: [],
      }),
    ),
  );
}

function renderApp(route: string) {
  return renderWithProviders(
    <SessionProvider>
      <App />
    </SessionProvider>,
    route,
  );
}

/**
 * Os testes de tela montam cada página com um `Routes` próprio, então um caminho errado no
 * App passaria por todos eles. Estes exercitam o registro real das rotas.
 */
describe('App', () => {
  it('abre a lista de pessoas', async () => {
    serveSession();

    renderApp('/pessoas');

    expect(await screen.findByRole('heading', { name: 'Pessoas' })).toBeInTheDocument();
  });

  it('abre a lista de vacinas', async () => {
    serveSession();

    renderApp('/vacinas');

    expect(await screen.findByRole('heading', { name: 'Vacinas' })).toBeInTheDocument();
  });

  it('abre o cartao de uma pessoa', async () => {
    serveSession();

    renderApp('/pessoas/p1/cartao');

    expect(await screen.findByRole('heading', { name: 'Maria Silva' })).toBeInTheDocument();
  });

  it('leva a raiz para a lista de pessoas', async () => {
    serveSession();

    renderApp('/');

    expect(await screen.findByRole('heading', { name: 'Pessoas' })).toBeInTheDocument();
  });

  it('leva um caminho desconhecido para a raiz', async () => {
    serveSession();

    renderApp('/caminho-que-nao-existe');

    expect(await screen.findByRole('heading', { name: 'Pessoas' })).toBeInTheDocument();
  });

  it('manda para o login quem nao tem sessao', async () => {
    server.use(
      http.get('*/api/auth/me', () => HttpResponse.json({ status: 401 }, { status: 401 })),
    );

    renderApp('/pessoas');

    expect(await screen.findByRole('heading', { name: 'Entrar' })).toBeInTheDocument();
  });

  it('mostra a moldura com o e-mail e a saida nas rotas com sessao', async () => {
    serveSession();

    renderApp('/pessoas');

    expect(await screen.findByText('maria@teste.local')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Sair' })).toBeInTheDocument();
  });

  it('encerra a sessao mesmo quando o logout falha', async () => {
    serveSession();
    server.use(http.post('*/api/auth/logout', () => HttpResponse.error()));

    renderApp('/pessoas');

    await userEvent.click(await screen.findByRole('button', { name: 'Sair' }));

    // O clique em Sair é uma decisão do usuário, não um pedido de tentativa: se a chamada
    // falhar por rede, ficar na aplicação com a sessão de pé é o pior desfecho possível.
    expect(await screen.findByRole('heading', { name: 'Entrar' })).toBeInTheDocument();
  });

  it('marca na navegacao a tela em que se esta', async () => {
    serveSession();

    renderApp('/vacinas');

    // `aria-current="page"` é o que o realce visual usa como gancho e o que o leitor de tela
    // anuncia: um só atributo cobre os dois.
    expect(await screen.findByRole('link', { name: 'Vacinas' })).toHaveAttribute(
      'aria-current',
      'page',
    );
    expect(screen.getByRole('link', { name: 'Pessoas' })).not.toHaveAttribute('aria-current');
  });
});
