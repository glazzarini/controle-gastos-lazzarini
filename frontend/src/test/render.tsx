import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { App } from '../App';
import { AppDataProvider } from '../state/AppData';
import { UiProvider } from '../state/Ui';

export type NomeTela = 'painel' | 'lancar' | 'lancamentos' | 'orcamento' | 'historico' | 'detalhe';

/** Todas as telas ficam montadas (como no index.html); as buscas são feitas dentro de uma tela. */
export const tela = (nome: NomeTela) => within(screen.getByTestId(`screen-${nome}`));

/** Renderiza o app completo e espera a primeira carga da API. */
export async function renderApp() {
  const usuario = userEvent.setup();
  const resultado = render(<UiProvider><AppDataProvider><App /></AppDataProvider></UiProvider>);
  await tela('painel').findByText('Por categoria');
  return { usuario, ...resultado };
}

/**
 * Valor em BRL como aparece para as queries do Testing Library: o Intl usa espaço não separável
 * e o normalizador padrão o converte em espaço comum.
 */
export const brl = (valor: number) =>
  new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(valor).replace(/\u00a0/g, ' ');

/** Toast atual. */
export const toast = () => screen.getByRole('status');
