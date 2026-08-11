import { describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';

import { server } from '../../test/server';
import { renderWithProviders } from '../../test/renderWithProviders';
import { PersonDialog } from './PersonDialog';

const rota = '*/api/people';

describe('PersonDialog', () => {
  it('mascara o CPF enquanto se digita', async () => {
    renderWithProviders(<PersonDialog open onClose={vi.fn()} />);

    const campo = screen.getByLabelText(/cpf/i);
    await userEvent.type(campo, '12345678901');

    expect(campo).toHaveValue('123.456.789-01');
  });

  it('envia o CPF sem mascara para a API', async () => {
    // A API grava e valida 11 dígitos; a máscara é só da tela.
    let enviado: { name: string; document: string } | null = null;

    server.use(
      http.post(rota, async ({ request }) => {
        enviado = (await request.json()) as { name: string; document: string };
        return HttpResponse.json({ id: 'nova', ...enviado }, { status: 201 });
      }),
    );

    renderWithProviders(<PersonDialog open onClose={vi.fn()} />);

    await userEvent.type(screen.getByLabelText(/nome/i), 'Maria Silva');
    await userEvent.type(screen.getByLabelText(/cpf/i), '123.456.789-01');
    await userEvent.click(screen.getByRole('button', { name: /salvar/i }));

    await waitFor(() => {
      expect(enviado).toEqual({ name: 'Maria Silva', document: '12345678901' });
    });
  });

  it('nao deixa passar de 11 digitos', async () => {
    renderWithProviders(<PersonDialog open onClose={vi.fn()} />);

    const campo = screen.getByLabelText(/cpf/i);
    await userEvent.type(campo, '123456789012345');

    expect(campo).toHaveValue('123.456.789-01');
  });

  it('leva o erro de validacao ao campo do CPF', async () => {
    // A chave vem como 'Document', em PascalCase; o campo do formulário é 'document'.
    server.use(
      http.post(rota, () =>
        HttpResponse.json(
          {
            status: 400,
            errors: { Document: ["'Documento' deve ser maior ou igual a 11 caracteres."] },
          },
          { status: 400 },
        ),
      ),
    );

    renderWithProviders(<PersonDialog open onClose={vi.fn()} />);

    await userEvent.type(screen.getByLabelText(/nome/i), 'Maria');
    await userEvent.type(screen.getByLabelText(/cpf/i), '123');
    await userEvent.click(screen.getByRole('button', { name: /salvar/i }));

    expect(screen.getByLabelText(/cpf/i)).toHaveAccessibleDescription(
      "'Documento' deve ser maior ou igual a 11 caracteres.",
    );
  });

  it('mostra o conflito de CPF repetido em alerta, sem fechar', async () => {
    const aoFechar = vi.fn();

    server.use(
      http.post(rota, () =>
        HttpResponse.json(
          {
            status: 409,
            detail: "Já existe uma pessoa cadastrada com o documento '12345678901'.",
          },
          { status: 409 },
        ),
      ),
    );

    renderWithProviders(<PersonDialog open onClose={aoFechar} />);

    await userEvent.type(screen.getByLabelText(/nome/i), 'Maria');
    await userEvent.type(screen.getByLabelText(/cpf/i), '12345678901');
    await userEvent.click(screen.getByRole('button', { name: /salvar/i }));

    expect(
      await screen.findByText("Já existe uma pessoa cadastrada com o documento '12345678901'."),
    ).toBeInTheDocument();
    expect(aoFechar).not.toHaveBeenCalled();
    // O que foi digitado permanece, ainda mascarado.
    expect(screen.getByLabelText(/cpf/i)).toHaveValue('123.456.789-01');
  });
});
