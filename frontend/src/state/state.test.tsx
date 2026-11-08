import { act, render, renderHook, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { CONTAS_FIXAS_PADRAO, TETOS_PADRAO } from '../lib/defaults';
import { banco, falhar, lancamento } from '../test/servidor';
import { AppDataProvider, useAppData } from './AppData';
import { useNavegacao } from './Navegacao';
import { UiProvider, useUi } from './Ui';

function MostrarDados() {
  const d = useAppData();
  return <pre data-testid="dados">{JSON.stringify({ ...d, recarregar: undefined })}</pre>;
}

async function carregarDados() {
  render(<AppDataProvider><MostrarDados /></AppDataProvider>);
  await waitFor(() => expect(JSON.parse(screen.getByTestId('dados').textContent!).carregado).toBe(true));
  return JSON.parse(screen.getByTestId('dados').textContent!);
}

describe('hooks fora do provider', () => {
  it.each([
    ['useAppData', useAppData, 'useAppData fora do AppDataProvider'],
    ['useUi', useUi, 'useUi fora do UiProvider'],
    ['useNavegacao', useNavegacao, 'useNavegacao fora do App'],
  ])('%s lança erro', (_, hook, mensagem) => {
    vi.spyOn(console, 'error').mockImplementation(() => {});
    expect(() => renderHook(() => hook())).toThrow(mensagem);
  });
});

describe('AppDataProvider', () => {
  it('carrega config, categorias e lançamentos ordenados por data desc', async () => {
    lancamento({ data: '01/06/2026', valor: 1, descricao: null, observacao: null, tipo: null, categoria: '' });
    lancamento({ data: '10/06/2026', valor: 2 });

    const dados = await carregarDados();

    expect(dados.config).toEqual({ renda_guilherme: 20000, renda_esposa: 14000, meta_fatura: 12000, total_fixos: 1724 });
    expect(Object.keys(dados.tetos)).toEqual(['Alimentacao fora', 'Guitarra', 'Pet']);
    expect(dados.contasFixas).toHaveLength(2);
    expect(dados.categorias).toHaveLength(3);
    expect(dados.lancamentos.map((l: { valor: number }) => l.valor)).toEqual([2, 1]);
    expect(dados.lancamentos[1]).toMatchObject({ descricao: '', observacao: '', tipo: 'A vista', categoria: '' });
  });

  it('se a API falha, mantém os valores locais e loga no console', async () => {
    const log = vi.spyOn(console, 'error').mockImplementation(() => {});
    falhar('get', '/api/config');
    falhar('get', '/api/categorias');
    falhar('get', '/api/lancamentos');

    const dados = await carregarDados();

    expect(dados.config.renda_guilherme).toBe(20000);
    expect(dados.tetos).toEqual(TETOS_PADRAO);
    expect(dados.contasFixas).toEqual(CONTAS_FIXAS_PADRAO);
    expect(dados.categorias).toEqual([]);
    expect(dados.lancamentos).toEqual([]);
    expect(log).toHaveBeenCalledTimes(3);
  });

  it('tetos/contas fixas vazios na API mantêm os padrões locais (como no index.html)', async () => {
    banco.categorias = [];
    banco.contasFixas = [];
    const dados = await carregarDados();
    expect(dados.tetos).toEqual(TETOS_PADRAO);
    expect(dados.contasFixas).toEqual(CONTAS_FIXAS_PADRAO);
    expect(dados.config.total_fixos).toBe(0);
  });

  it('renda 0 é respeitada', async () => {
    banco.config.renda_esposa = 0;
    expect((await carregarDados()).config.renda_esposa).toBe(0);
  });
});

describe('UiProvider', () => {
  function Botoes({ aoConfirmar }: { aoConfirmar: () => void }) {
    const { toast, confirmar } = useUi();
    return (
      <>
        <button onClick={() => toast('Olá')}>toast</button>
        <button onClick={() => toast('Falhou', 'error')}>erro</button>
        <button onClick={() => confirmar('Título', 'Texto', aoConfirmar)}>abrir</button>
      </>
    );
  }

  it('toast aparece e some após 2,5s; um novo toast reinicia o tempo', async () => {
    vi.useFakeTimers();
    render(<UiProvider><Botoes aoConfirmar={() => {}} /></UiProvider>);
    const status = screen.getByRole('status');

    act(() => screen.getByText('toast').click());
    expect(status).toHaveClass('toast', 'success', 'show');
    expect(status).toHaveTextContent('Olá');

    act(() => vi.advanceTimersByTime(2000));
    act(() => screen.getByText('erro').click());
    act(() => vi.advanceTimersByTime(2000));
    expect(status).toHaveClass('error', 'show');

    act(() => vi.advanceTimersByTime(600));
    expect(status).not.toHaveClass('show');
  });

  it('confirmação: Cancelar fecha sem executar; Confirmar executa e fecha', async () => {
    const usuario = userEvent.setup();
    const aoConfirmar = vi.fn();
    render(<UiProvider><Botoes aoConfirmar={aoConfirmar} /></UiProvider>);
    const dialogo = screen.getByRole('dialog', { hidden: true });

    const ok = () => screen.getByRole('button', { name: 'Confirmar', hidden: true });
    await usuario.click(ok()); // nada aberto: não faz nada
    expect(aoConfirmar).not.toHaveBeenCalled();

    await usuario.click(screen.getByText('abrir'));
    expect(dialogo).toHaveClass('open');
    expect(dialogo).toHaveTextContent('TítuloTexto');
    await usuario.click(screen.getByText('Cancelar'));
    expect(dialogo).not.toHaveClass('open');
    expect(aoConfirmar).not.toHaveBeenCalled();

    await usuario.click(screen.getByText('abrir'));
    await usuario.click(ok());
    expect(aoConfirmar).toHaveBeenCalledTimes(1);
    expect(dialogo).not.toHaveClass('open');
  });
});
