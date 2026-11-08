import { useCallback, useMemo } from 'react';
import { lancamentosDoMes, totaisPorCategoria } from '../lib/calculos';
import { corValorLancamento } from '../lib/cores';
import type { Lancamento } from '../lib/types';
import { useAppData } from './AppData';
import { useNavegacao } from './Navegacao';

/** Lançamentos e totais por categoria do mês selecionado. */
export function useMesAtual() {
  const { lancamentos, tetos } = useAppData();
  const { mes } = useNavegacao();
  return useMemo(() => {
    const doMes = lancamentosDoMes(lancamentos, mes);
    return { doMes, totais: totaisPorCategoria(doMes, tetos) };
  }, [lancamentos, tetos, mes]);
}

/** Cor do valor de um lançamento conforme o consumo da categoria no mês selecionado. */
export function useCorLancamento() {
  const { tetos } = useAppData();
  const { totais } = useMesAtual();
  return useCallback(
    (l: Lancamento) => corValorLancamento(l.valor, totais[l.categoria] || 0, tetos[l.categoria] || 0),
    [totais, tetos]);
}

/** Nomes das categorias (chaves de tetos) em ordem alfabética — opções dos selects. */
export function useCategoriasOrdenadas(): string[] {
  const { tetos } = useAppData();
  return useMemo(() => Object.keys(tetos).sort((a, b) => a.localeCompare(b)), [tetos]);
}
