import { describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';

import { server } from '../../test/server';
import { renderWithProviders } from '../../test/renderWithProviders';
import { VaccineDialog } from './VaccineDialog';

const rota = '*/api/vaccines';

describe('VaccineDialog', () => {
  it('leva o erro de validacao ao campo, apesar da chave em PascalCase', async () => {
    // A API devolve 'Name'; o campo do formulário é 'name'. Sem a normalização da primeira
    // letra o erro não encontra o campo e some da tela sem quebrar nada.
    server.use(
      http.post(rota, () =>
        HttpResponse.json(
          {
            title: 'Requisição inválida',
            status: 400,
            errors: { Name: ["'Nome' deve ser informado."] },
          },
          { status: 400 },
        ),
      ),
    );

    renderWithProviders(<VaccineDialog open onClose={vi.fn()} />);

    await userEvent.click(screen.getByRole('button', { name: /salvar/i }));

    expect(await screen.findByText("'Nome' deve ser informado.")).toBeInTheDocument();
    // O texto precisa estar ligado ao campo, e não solto num alerta qualquer.
    expect(screen.getByLabelText(/nome/i)).toHaveAccessibleDescription(
      "'Nome' deve ser informado.",
    );
  });

  it('mostra o conflito de nome repetido em alerta, sem fechar o dialogo', async () => {
    // 409 não traz dicionário de campos: a mensagem vai para o Alert e o que foi digitado fica.
    const aoFechar = vi.fn();

    server.use(
      http.post(rota, () =>
        HttpResponse.json(
          {
            title: 'Conflito com o estado atual',
            status: 409,
            detail: "Já existe uma vacina cadastrada com o nome 'BCG'.",
          },
          { status: 409 },
        ),
      ),
    );

    renderWithProviders(<VaccineDialog open onClose={aoFechar} />);

    await userEvent.type(screen.getByLabelText(/nome/i), 'BCG');
    await userEvent.click(screen.getByRole('button', { name: /salvar/i }));

    expect(
      await screen.findByText("Já existe uma vacina cadastrada com o nome 'BCG'."),
    ).toBeInTheDocument();
    expect(aoFechar).not.toHaveBeenCalled();
    expect(screen.getByLabelText(/nome/i)).toHaveValue('BCG');
  });

  it('fecha e limpa o formulario apos o cadastro', async () => {
    const aoFechar = vi.fn();

    server.use(
      http.post(rota, () => HttpResponse.json({ id: 'nova', name: 'Febre Amarela' }, { status: 201 })),
    );

    renderWithProviders(<VaccineDialog open onClose={aoFechar} />);

    await userEvent.type(screen.getByLabelText(/nome/i), 'Febre Amarela');
    await userEvent.click(screen.getByRole('button', { name: /salvar/i }));

    await vi.waitFor(() => {
      expect(aoFechar).toHaveBeenCalledOnce();
    });
    expect(screen.getByLabelText(/nome/i)).toHaveValue('');
  });
});
