import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import type { Categoria, ContaFixa, LancamentoApi } from '../lib/types';

/** Backend fake em memória que segue o contrato da API (.NET) — usado por todos os testes. */
export interface BancoFake {
  config: { renda_guilherme: number; renda_esposa: number; meta_fatura: number };
  categorias: Categoria[];
  contasFixas: ContaFixa[];
  lancamentos: LancamentoApi[];
}

export interface Requisicao { metodo: string; caminho: string; corpo: unknown }

let sequencia = 0;
const novoId = (prefixo: string) => `${prefixo}-${++sequencia}`;

export function criarBanco(): BancoFake {
  return {
    config: { renda_guilherme: 20000, renda_esposa: 14000, meta_fatura: 12000 },
    categorias: [
      { id: 'cat-pet', nome: 'Pet', teto: 450, created_at: null },
      { id: 'cat-ali', nome: 'Alimentacao fora', teto: 2000, created_at: null },
      { id: 'cat-gui', nome: 'Guitarra', teto: 300, created_at: null },
    ],
    contasFixas: [
      { id: 'cf-1', nome: 'Condomínio', valor: 1600, created_at: null },
      { id: 'cf-2', nome: 'Vivo', valor: 124, created_at: null },
    ],
    lancamentos: [],
  };
}

export const banco: BancoFake = criarBanco();
export const requisicoes: Requisicao[] = [];

export function resetarBanco(parcial: Partial<BancoFake> = {}): void {
  Object.assign(banco, criarBanco(), parcial);
  requisicoes.length = 0;
}

/** Cria um lançamento direto no "banco" (data DD/MM/YYYY). */
export function lancamento(dados: Partial<LancamentoApi> & { data: string; valor: number }): LancamentoApi {
  const l: LancamentoApi = {
    id: novoId('l'), descricao: '', categoria: 'Pet', observacao: null, tipo: 'A vista', created_at: null, ...dados,
  };
  banco.lancamentos.push(l);
  return l;
}

const erro = (status: number, mensagem: string, extras: object = {}) =>
  HttpResponse.json({ error: mensagem, ...extras }, { status });

const paraIso = (br: string) => br.split('/').reverse().join('-');

function adicionarMesesBr(br: string, meses: number): string {
  const [d, m, y] = br.split('/').map(Number);
  const alvo = new Date(y!, m! - 1 + meses, 1);
  const ultimo = new Date(alvo.getFullYear(), alvo.getMonth() + 1, 0).getDate();
  const dia = Math.min(d!, ultimo);
  return `${String(dia).padStart(2, '0')}/${String(alvo.getMonth() + 1).padStart(2, '0')}/${alvo.getFullYear()}`;
}

async function registrar(request: Request): Promise<any> {
  const texto = await request.text();
  const corpo = texto ? JSON.parse(texto) : undefined;
  requisicoes.push({ metodo: request.method, caminho: new URL(request.url).pathname, corpo });
  return corpo;
}

export const handlers = [
  http.get('/api/config', async ({ request }) => {
    await registrar(request);
    const categorias = [...banco.categorias].sort((a, b) => a.nome.localeCompare(b.nome));
    return HttpResponse.json({
      ...banco.config,
      total_fixos: banco.contasFixas.reduce((s, c) => s + c.valor, 0),
      tetos: Object.fromEntries(categorias.map(c => [c.nome, c.teto])),
      contas_fixas: banco.contasFixas.map(({ id, nome, valor }) => ({ id, nome, valor })),
    });
  }),
  http.put('/api/config', async ({ request }) => {
    const corpo = await registrar(request);
    Object.assign(banco.config, corpo);
    return HttpResponse.json({ updated: true });
  }),

  http.get('/api/categorias', async ({ request }) => {
    await registrar(request);
    return HttpResponse.json([...banco.categorias].sort((a, b) => a.nome.localeCompare(b.nome)));
  }),
  http.post('/api/categorias', async ({ request }) => {
    const { nome, teto } = await registrar(request);
    if (banco.categorias.some(c => c.nome === nome)) return erro(409, 'Já existe uma categoria com esse nome');
    const nova = { id: novoId('cat'), nome, teto, created_at: null };
    banco.categorias.push(nova);
    return HttpResponse.json(nova, { status: 201 });
  }),
  http.put('/api/categorias/:id', async ({ request, params }) => {
    const { nome, teto } = await registrar(request);
    const cat = banco.categorias.find(c => c.id === params.id);
    if (!cat) return erro(404, 'Categoria não encontrada');
    let atualizados = 0;
    if (nome !== cat.nome) {
      banco.lancamentos.filter(l => l.categoria === cat.nome).forEach(l => { l.categoria = nome; atualizados++; });
    }
    Object.assign(cat, { nome, teto });
    return HttpResponse.json({ updated: true, categoria: cat, lancamentos_atualizados: atualizados });
  }),
  http.delete('/api/categorias/:id', async ({ request, params }) => {
    await registrar(request);
    const cat = banco.categorias.find(c => c.id === params.id)!;
    const emUso = banco.lancamentos.filter(l => l.categoria === cat.nome).length;
    if (emUso > 0) {
      return erro(409, `Não é possível excluir: ${emUso} lançamento(s) usam essa categoria`, { lancamentos_em_uso: emUso });
    }
    banco.categorias = banco.categorias.filter(c => c !== cat);
    return HttpResponse.json({ deleted: true });
  }),

  http.post('/api/contas-fixas', async ({ request }) => {
    const { nome, valor } = await registrar(request);
    const nova = { id: novoId('cf'), nome, valor, created_at: null };
    banco.contasFixas.push(nova);
    return HttpResponse.json(nova, { status: 201 });
  }),
  http.put('/api/contas-fixas/:id', async ({ request, params }) => {
    const corpo = await registrar(request);
    const cf = banco.contasFixas.find(c => c.id === params.id)!;
    Object.assign(cf, corpo);
    return HttpResponse.json({ updated: true, conta_fixa: cf });
  }),
  http.delete('/api/contas-fixas/:id', async ({ request, params }) => {
    await registrar(request);
    banco.contasFixas = banco.contasFixas.filter(c => c.id !== params.id);
    return HttpResponse.json({ deleted: true });
  }),

  http.get('/api/lancamentos', async ({ request }) => {
    await registrar(request);
    const ordenados = [...banco.lancamentos].sort((a, b) => paraIso(b.data!).localeCompare(paraIso(a.data!)));
    return HttpResponse.json(ordenados);
  }),
  http.post('/api/lancamentos', async ({ request }) => {
    const c = await registrar(request);
    const n = c.parcelas > 1 ? c.parcelas : 1;
    const ids: string[] = [];
    for (let i = 0; i < n; i++) {
      const l = lancamento({
        data: adicionarMesesBr(c.data, i),
        descricao: n > 1 ? `${c.descricao} (${i + 1}/${n})`.trim() : c.descricao,
        categoria: c.categoria,
        valor: n > 1 ? Math.round((c.valor / n) * 100) / 100 : c.valor,
        observacao: c.observacao || null,
        tipo: c.tipo || 'A vista',
      });
      ids.push(l.id);
    }
    return HttpResponse.json({ inserted: n, ids }, { status: 201 });
  }),
  http.put('/api/lancamentos/:id', async ({ request, params }) => {
    const corpo = await registrar(request);
    const l = banco.lancamentos.find(x => x.id === params.id)!;
    Object.assign(l, corpo);
    return HttpResponse.json({ updated: true, lancamento: l });
  }),
  http.delete('/api/lancamentos/:id', async ({ request, params }) => {
    await registrar(request);
    banco.lancamentos = banco.lancamentos.filter(l => l.id !== params.id);
    return HttpResponse.json({ deleted: true });
  }),
];

export const servidor = setupServer(...handlers);

type Metodo = 'get' | 'post' | 'put' | 'delete';

/** Faz a próxima chamada (ou todas) a uma rota falhar com { error }. */
export function falhar(metodo: Metodo, caminho: string, status = 500, mensagem = 'falha simulada', once = true): void {
  servidor.use(http[metodo](caminho, () => erro(status, mensagem), { once }));
}
