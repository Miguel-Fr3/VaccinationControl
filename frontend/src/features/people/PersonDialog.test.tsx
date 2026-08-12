import { describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';

import { server } from '../../test/server';
import { renderWithProviders } from '../../test/renderWithProviders';
import { PersonDialog } from './PersonDialog';

const route = '*/api/people';

describe('PersonDialog', () => {
  it('mascara o CPF enquanto se digita', async () => {
    renderWithProviders(<PersonDialog open onClose={vi.fn()} />);

    const field = screen.getByLabelText(/cpf/i);
    await userEvent.type(field, '12345678901');

    expect(field).toHaveValue('123.456.789-01');
  });

  it('envia o CPF sem mascara para a API', async () => {
    // A API grava e valida 11 dígitos; a máscara é só da tela.
    let sent: { name: string; document: string } | null = null;

    server.use(
      http.post(route, async ({ request }) => {
        sent = (await request.json()) as { name: string; document: string };
        return HttpResponse.json({ id: 'nova', ...sent }, { status: 201 });
      }),
    );

    renderWithProviders(<PersonDialog open onClose={vi.fn()} />);

    await userEvent.type(screen.getByLabelText(/nome/i), 'Maria Silva');
    await userEvent.type(screen.getByLabelText(/cpf/i), '111.444.777-35');
    await userEvent.click(screen.getByRole('button', { name: /salvar/i }));

    await waitFor(() => {
      expect(sent).toEqual({ name: 'Maria Silva', document: '11144477735' });
    });
  });

  it('recusa CPF com digito verificador errado antes de chamar a API', async () => {
    // A conta é a mesma do backend; rodá-la aqui evita a ida ao servidor para um número
    // que já se sabe impossível.
    let called = false;

    server.use(
      http.post(route, () => {
        called = true;
        return HttpResponse.json({ id: 'nova' }, { status: 201 });
      }),
    );

    renderWithProviders(<PersonDialog open onClose={vi.fn()} />);

    await userEvent.type(screen.getByLabelText(/nome/i), 'Maria Silva');
    await userEvent.type(screen.getByLabelText(/cpf/i), '11144477736');
    await userEvent.click(screen.getByRole('button', { name: /salvar/i }));

    expect(
      await screen.findByText('Informe um CPF válido, com onze dígitos.'),
    ).toBeInTheDocument();
    expect(called).toBe(false);
  });

  it('nao deixa passar de 11 digitos', async () => {
    renderWithProviders(<PersonDialog open onClose={vi.fn()} />);

    const field = screen.getByLabelText(/cpf/i);
    await userEvent.type(field, '123456789012345');

    expect(field).toHaveValue('123.456.789-01');
  });

  it('leva o erro de validacao ao campo do CPF', async () => {
    // A chave vem como 'Document', em PascalCase; o campo do formulário é 'document'.
    // O CPF digitado passa na conferência da tela: o que se testa aqui é o caminho de volta
    // de um 400, que continua existindo para toda regra que só o servidor conhece.
    server.use(
      http.post(route, () =>
        HttpResponse.json(
          { status: 400, errors: { Document: ["'CPF' informado não é válido."] } },
          { status: 400 },
        ),
      ),
    );

    renderWithProviders(<PersonDialog open onClose={vi.fn()} />);

    await userEvent.type(screen.getByLabelText(/nome/i), 'Maria');
    await userEvent.type(screen.getByLabelText(/cpf/i), '11144477735');
    await userEvent.click(screen.getByRole('button', { name: /salvar/i }));

    expect(await screen.findByText("'CPF' informado não é válido.")).toBeInTheDocument();
    expect(screen.getByLabelText(/cpf/i)).toHaveAccessibleDescription(
      "'CPF' informado não é válido.",
    );
  });

  it('mostra o conflito de CPF repetido em alerta, sem fechar', async () => {
    const onClose = vi.fn();

    server.use(
      http.post(route, () =>
        HttpResponse.json(
          {
            status: 409,
            detail: "Já existe uma pessoa cadastrada com o documento '11144477735'.",
          },
          { status: 409 },
        ),
      ),
    );

    renderWithProviders(<PersonDialog open onClose={onClose} />);

    await userEvent.type(screen.getByLabelText(/nome/i), 'Maria');
    await userEvent.type(screen.getByLabelText(/cpf/i), '11144477735');
    await userEvent.click(screen.getByRole('button', { name: /salvar/i }));

    expect(
      await screen.findByText("Já existe uma pessoa cadastrada com o documento '11144477735'."),
    ).toBeInTheDocument();
    expect(onClose).not.toHaveBeenCalled();
    // O que foi digitado permanece, ainda mascarado.
    expect(screen.getByLabelText(/cpf/i)).toHaveValue('111.444.777-35');
  });
});
