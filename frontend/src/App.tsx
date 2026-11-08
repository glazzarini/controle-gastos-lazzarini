import { useCallback, useMemo, useState } from 'react';
import { BottomNav, type TelaNav } from './components/BottomNav';
import { MonthPicker } from './components/MonthPicker';
import { TopBar } from './components/TopBar';
import { api } from './lib/api';
import { lancamentosDoMes } from './lib/calculos';
import { formatDate, formatMoney } from './lib/format';
import { MESES, mesCorrente, type Mes } from './lib/meses';
import type { Lancamento } from './lib/types';
import { DetalheLancamento } from './pages/DetalheLancamento';
import { Historico } from './pages/Historico';
import { Lancamentos } from './pages/Lancamentos';
import { Lancar } from './pages/Lancar';
import { Orcamento } from './pages/Orcamento';
import { Painel } from './pages/Painel';
import { useAppData } from './state/AppData';
import { NavegacaoContext, type Navegacao, type Tela } from './state/Navegacao';
import { useUi } from './state/Ui';

export function App() {
  const { lancamentos, recarregar } = useAppData();
  const { toast, confirmar } = useUi();

  // O app sempre abre no mês corrente.
  const [mes, setMes] = useState<Mes>(() => mesCorrente());
  const [tela, setTela] = useState<Tela>('painel');
  const [navAtiva, setNavAtiva] = useState<TelaNav>('painel');
  const [filtro, setFiltro] = useState('Todos');
  const [detalheId, setDetalheId] = useState<string | null>(null);
  const [seletorAberto, setSeletorAberto] = useState(false);
  const [atualizando, setAtualizando] = useState(false);

  const irPara = useCallback((destino: TelaNav) => {
    if (destino === 'lancamentos') setFiltro('Todos');
    setTela(destino);
    setNavAtiva(destino);
    window.scrollTo(0, 0);
  }, []);

  const abrirDetalhe = useCallback((id: string) => {
    setDetalheId(id);
    setTela('detalhe');
    window.scrollTo(0, 0);
  }, []);

  const navegacao = useMemo<Navegacao>(() => ({ tela, mes, irPara, abrirDetalhe }), [tela, mes, irPara, abrirDetalhe]);

  const selecionarMes = (novo: Mes) => {
    setMes(novo);
    setSeletorAberto(false);
    if (tela === 'lancamentos') setFiltro('Todos');
  };

  const atualizar = async () => {
    setAtualizando(true);
    try {
      await recarregar();
      toast('Atualizado ✓', 'success');
    } finally {
      setAtualizando(false);
    }
  };

  const excluirLancamento = (l: Lancamento) => confirmar(
    'Excluir lançamento',
    `Excluir "${l.descricao || l.categoria}" de ${formatMoney(l.valor)} em ${formatDate(l.data)}?`,
    async () => {
      toast('Excluindo...', 'success');
      try {
        await api.excluirLancamento(l.id);
        await recarregar();
        toast('Lançamento excluído ✓', 'success');
        if (tela === 'detalhe') irPara('lancamentos');
      } catch (e) {
        toast(`Erro ao excluir: ${(e as Error).message}`, 'error');
      }
    });

  const limparMes = (m: Mes) => {
    const doMes = lancamentosDoMes(lancamentos, m);
    if (doMes.length === 0) {
      toast('Nenhum lançamento neste mês', 'error');
      return;
    }
    confirmar(
      `Limpar ${m.label}`,
      `Isso vai excluir todos os ${doMes.length} lançamento(s) de ${m.label}. Essa ação não pode ser desfeita.`,
      async () => {
        toast('Excluindo...', 'success');
        try {
          for (const l of doMes) {
            await api.excluirLancamento(l.id);
          }
          await recarregar();
          toast(`${m.label} limpo ✓`, 'success');
        } catch (e) {
          toast(`Erro ao limpar mês: ${(e as Error).message}`, 'error');
        }
      });
  };

  const classeTela = (t: Tela) => `screen${tela === t ? ' active' : ''}`;

  return (
    <NavegacaoContext.Provider value={navegacao}>
      <TopBar mesLabel={mes.label} atualizando={atualizando} onAbrirMeses={() => setSeletorAberto(true)} onAtualizar={atualizar} />

      <div className="content">
        <div className={classeTela('painel')} data-testid="screen-painel"><Painel /></div>
        <div className={classeTela('lancar')} data-testid="screen-lancar"><Lancar /></div>
        <div className={classeTela('lancamentos')} data-testid="screen-lancamentos">
          <Lancamentos filtro={filtro} onFiltrar={setFiltro} onExcluir={excluirLancamento} />
        </div>
        <div className={classeTela('orcamento')} data-testid="screen-orcamento"><Orcamento /></div>
        <div className={classeTela('historico')} data-testid="screen-historico"><Historico onLimparMes={limparMes} /></div>
        <div className={classeTela('detalhe')} data-testid="screen-detalhe">
          <DetalheLancamento id={detalheId} onExcluir={excluirLancamento} />
        </div>
      </div>

      <BottomNav ativa={navAtiva} onIr={irPara} />
      <MonthPicker aberto={seletorAberto} meses={MESES} atual={mes} onSelecionar={selecionarMes} onFechar={() => setSeletorAberto(false)} />
    </NavegacaoContext.Provider>
  );
}
