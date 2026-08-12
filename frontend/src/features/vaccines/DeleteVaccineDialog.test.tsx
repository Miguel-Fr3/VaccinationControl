import { describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';

import { server } from '../../test/server';
import { renderWithProviders } from '../../test/renderWithProviders';
import type { Vaccine } from '../../api/types';
import { DeleteVaccineDialog } from './DeleteVaccineDialog';

const vaccine: Vaccine = { id: 'v1', name: 'Hepatite B' };

const vaccineRoute = '*/api/vaccines/v1';

describe('DeleteVaccineDialog', () => {
  it('avisa que vacina em uso nao pode ser excluida', () => {
    renderWithProviders(<DeleteVaccineDialog vaccine={vaccine} onClose={vi.fn()} />);

    expect(screen.getByText(/hepatite b/i)).toBeInTheDocument();
    expect(screen.getByText(/não podem ser excluídas/i)).toBeInTheDocument();
    expect(screen.getByText(/não pode ser desfeita/i)).toBeInTheDocument();
  });

  it('exclui e fecha ao confirmar', async () => {
    const onClose = vi.fn();
    let deleted = false;

    server.use(
      http.delete(vaccineRoute, () => {
        deleted = true;
        return new HttpResponse(null, { status: 204 });
      }),
    );

    renderWithProviders(<DeleteVaccineDialog vaccine={vaccine} onClose={onClose} />);

    await userEvent.click(screen.getByRole('button', { name: /excluir/i }));

    await waitFor(() => {
      expect(onClose).toHaveBeenCalledOnce();
    });
    expect(deleted).toBe(true);
  });

  it('mostra o 409 da API e mantem o dialogo aberto', async () => {
    const onClose = vi.fn();

    server.use(
      http.delete(vaccineRoute, () =>
        HttpResponse.json(
          {
            status: 409,
            detail: "A vacina 'Hepatite B' tem doses registradas e não pode ser removida.",
          },
          { status: 409 },
        ),
      ),
    );

    renderWithProviders(<DeleteVaccineDialog vaccine={vaccine} onClose={onClose} />);

    await userEvent.click(screen.getByRole('button', { name: /excluir/i }));

    expect(
      await screen.findByText(/tem doses registradas e não pode ser removida/i),
    ).toBeInTheDocument();
    expect(onClose).not.toHaveBeenCalled();
  });
});
