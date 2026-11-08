import { useState } from 'react';
import { Carregando } from '../components/Carregando';
import { FormModal, type ConfigFormModal } from '../components/FormModal';
import { IconeLapis, IconeLixeira } from '../components/Icones';
import { metaFatura, resumoOrcamento, soma } from '../lib/calculos';
import { corFaturaOrcamento, corPoupanca, corSinal, variavelCss } from '../lib/cores';
import { iconeCategoria } from '../lib/defaults';
import { api } from '../lib/api';
import { formatMoney, percentual } from '../lib/format';
import type { Categoria, ContaFixaResumo } from '../lib/types';
import { useAppData } from '../state/AppData';
import { useMesAtual } from '../state/hooks';
import { useNavegacao } from '../state/Navegacao';
import { useUi } from '../state/Ui';

function BotaoEditar({ titulo, onClick }: { titulo: string; onClick: () => void }) {
  return <button className="orc-action-btn" onClick={onClick} title={titulo} aria-label={titulo}><IconeLapis /></button>;
}

function BotaoExcluir({ titulo, onClick }: { titulo: string; onClick: () => void }) {
  return <button className="orc-action-btn danger" onClick={onClick} title={titulo} aria-label={titulo}><IconeLixeira /></button>;
}

export function Orcamento() {
  const { config, contasFixas, categorias, carregado, recarregar } = useAppData();
  const { mes } = useNavegacao();
  const { doMes } = useMesAtual();
  const { toast, confirmar } = useUi();
  const [form, setForm] = useState<ConfigFormModal | null>(null);

  if (!carregado) return <Carregando />;

  const resumo = resumoOrcamento(config, soma(doMes.map(l => l.valor)));
  const meta = metaFatura(config);

  const editarRenda = () => setForm({
    titulo: 'Editar renda',
    campos: [
      { chave: 'renda_guilherme', rotulo: 'Guilherme (PJ líquido)', tipo: 'money', valor: config.renda_guilherme },
      { chave: 'renda_esposa', rotulo: 'Esposa (PJ líquido)', tipo: 'money', valor: config.renda_esposa },
    ],
    aoSalvar: async v => {
      const rg = parseFloat(v.renda_guilherme!);
      const re = parseFloat(v.renda_esposa!);
      if (isNaN(rg) || rg < 0 || isNaN(re) || re < 0) throw new Error('Informe valores válidos');
      await api.atualizarRenda({ renda_guilherme: rg, renda_esposa: re });
      await recarregar();
      toast('Renda atualizada ✓', 'success');
    },
  });

  const editarContaFixa = (cf: ContaFixaResumo | null) => setForm({
    titulo: cf ? 'Editar conta fixa' : 'Nova conta fixa',
    campos: [
      { chave: 'nome', rotulo: 'Nome', tipo: 'text', valor: cf ? cf.nome : '' },
      { chave: 'valor', rotulo: 'Valor', tipo: 'money', valor: cf ? cf.valor : '' },
    ],
    aoSalvar: async v => {
      const nome = v.nome!.trim();
      const valor = parseFloat(v.valor!);
      if (!nome) throw new Error('Informe o nome');
      if (isNaN(valor) || valor < 0) throw new Error('Valor inválido');
      if (cf) await api.atualizarContaFixa(cf.id!, { nome, valor });
      else await api.criarContaFixa({ nome, valor });
      await recarregar();
      toast(cf ? 'Conta fixa atualizada ✓' : 'Conta fixa criada ✓', 'success');
    },
  });

  const excluirContaFixa = (cf: ContaFixaResumo) => confirmar('Excluir conta fixa', `Excluir "${cf.nome}"?`, async () => {
    try {
      await api.excluirContaFixa(cf.id!);
      await recarregar();
      toast('Conta fixa excluída ✓', 'success');
    } catch (e) {
      toast(`Erro ao excluir: ${(e as Error).message}`, 'error');
    }
  });

  const editarCategoria = (cat: Categoria | null) => setForm({
    titulo: cat ? 'Editar categoria' : 'Nova categoria',
    campos: [
      { chave: 'nome', rotulo: 'Nome', tipo: 'text', valor: cat ? cat.nome : '' },
      { chave: 'teto', rotulo: 'Teto mensal', tipo: 'money', valor: cat ? cat.teto : '' },
    ],
    aoSalvar: async v => {
      const nome = v.nome!.trim();
      const teto = parseFloat(v.teto!);
      if (!nome) throw new Error('Informe o nome');
      if (isNaN(teto) || teto < 0) throw new Error('Teto inválido');
      let mensagem = cat ? 'Categoria atualizada ✓' : 'Categoria criada ✓';
      if (cat) {
        const resultado = await api.atualizarCategoria(cat.id, { nome, teto });
        if (resultado.lancamentos_atualizados > 0) {
          mensagem = `Categoria renomeada — ${resultado.lancamentos_atualizados} lançamento(s) do histórico atualizado(s) ✓`;
        }
      } else {
        await api.criarCategoria({ nome, teto });
      }
      await recarregar();
      toast(mensagem, 'success');
    },
  });

  const excluirCategoria = (cat: Categoria) => confirmar('Excluir categoria', `Excluir "${cat.nome}"?`, async () => {
    try {
      await api.excluirCategoria(cat.id);
      await recarregar();
      toast('Categoria excluída ✓', 'success');
    } catch (e) {
      toast((e as Error).message, 'error');
    }
  });

  const classeSobra = corSinal(resumo.sobra);
  const classePoupanca = corPoupanca(resumo.pctPoupanca);

  return (
    <>
      <div className="metric-row">
        <div className="metric">
          <div className="metric-label">Sobra / Poupança</div>
          <div className={`metric-value ${classeSobra}`}>{formatMoney(resumo.sobra)}</div>
          <div className="metric-sub">{percentual(resumo.pctPoupanca, 1)} da renda</div>
        </div>
        <div className="metric">
          <div className="metric-label">Meta poupança</div>
          <div className="metric-value green">{formatMoney(resumo.renda * 0.2)}</div>
          <div className="metric-sub">20% da renda</div>
        </div>
      </div>

      <div className="card">
        <div className="card-title" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          Renda
          <BotaoEditar titulo="Editar renda" onClick={editarRenda} />
        </div>
        <div className="orc-row">
          <div className="orc-label">💼 Guilherme (PJ líquido)</div>
          <div className="orc-value" style={{ color: 'var(--accent)' }}>{formatMoney(config.renda_guilherme)}</div>
        </div>
        <div className="orc-row">
          <div className="orc-label">💼 Esposa (PJ líquido)</div>
          <div className="orc-value" style={{ color: 'var(--accent)' }}>{formatMoney(config.renda_esposa)}</div>
        </div>
        <div className="orc-row total">
          <div className="orc-label">Renda total</div>
          <div className="orc-value">{formatMoney(resumo.renda)}</div>
        </div>
      </div>

      <div className="card">
        <div className="card-title">Contas fixas</div>
        {contasFixas.map(c => (
          <div className="orc-row" key={c.id ?? c.nome} data-testid="conta-fixa">
            <div className="orc-label">{c.icone || '💰'} {c.nome}</div>
            <div className="orc-row-actions">
              <div className="orc-value">{formatMoney(c.valor)}</div>
              {c.id && (
                <>
                  <BotaoEditar titulo={`Editar ${c.nome}`} onClick={() => editarContaFixa(c)} />
                  <BotaoExcluir titulo={`Excluir ${c.nome}`} onClick={() => excluirContaFixa(c)} />
                </>
              )}
            </div>
          </div>
        ))}
        <div className="orc-row total">
          <div className="orc-label">Total fixos</div>
          <div className="orc-value">{formatMoney(resumo.fixos)}</div>
        </div>
        <button className="add-row-btn" onClick={() => editarContaFixa(null)}>+ Adicionar conta fixa</button>
      </div>

      <div className="card">
        <div className="card-title">Categorias e tetos</div>
        {categorias.map(c => (
          <div className="orc-row" key={c.id} data-testid="categoria">
            <div className="orc-label">{iconeCategoria(c.nome)} {c.nome}</div>
            <div className="orc-row-actions">
              <div className="orc-value">{formatMoney(c.teto)}</div>
              <BotaoEditar titulo={`Editar ${c.nome}`} onClick={() => editarCategoria(c)} />
              <BotaoExcluir titulo={`Excluir ${c.nome}`} onClick={() => excluirCategoria(c)} />
            </div>
          </div>
        ))}
        <button className="add-row-btn" onClick={() => editarCategoria(null)}>+ Adicionar categoria</button>
      </div>

      <div className="card">
        <div className="card-title">Resultado {mes.label}</div>
        <div className="orc-row">
          <div className="orc-label">💳 Fatura cartão</div>
          <div className="orc-value" style={{ color: variavelCss(corFaturaOrcamento(resumo.totalFatura, meta)) }}>{formatMoney(resumo.totalFatura)}</div>
        </div>
        <div className="orc-row">
          <div className="orc-label">🎯 Meta fatura</div>
          <div className="orc-value" style={{ color: 'var(--accent)' }}>{formatMoney(meta)}</div>
        </div>
        <div className="orc-row total">
          <div className="orc-label">Sobra / Poupança</div>
          <div className={`orc-value ${classeSobra}`}>{formatMoney(resumo.sobra)}</div>
        </div>
        <div className="orc-row total">
          <div className="orc-label">% Poupança</div>
          <div className={`orc-value ${classePoupanca}`}>{percentual(resumo.pctPoupanca, 1)}</div>
        </div>
      </div>

      <FormModal config={form} onFechar={() => setForm(null)} />
    </>
  );
}
