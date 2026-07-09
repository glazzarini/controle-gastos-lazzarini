# SPEC — controle-gastos PWA

Controle de gastos do cartão de crédito da família Lazzarini.
Substitui Google Sheets + Apps Script por Supabase (PostgreSQL) + Vercel Functions (Node.js).
Frontend: HTML/CSS/JS puro, sem frameworks, sem bundler.

---

## Stack

| Camada | Tecnologia |
|---|---|
| Frontend | HTML + CSS + JS puro (single file `index.html`) |
| Backend | Vercel Serverless Functions (Node.js) |
| Banco | Supabase — PostgreSQL (`sa-east-1` São Paulo) |
| Hospedagem | Vercel (GitHub auto-deploy) |
| Migração | Script Node.js local (Google Sheets API → Supabase) |

---

## Banco de dados

### Tabela `lancamentos`

```sql
CREATE TABLE lancamentos (
  id          UUID DEFAULT gen_random_uuid() PRIMARY KEY,
  data        DATE NOT NULL,
  descricao   TEXT,
  categoria   TEXT NOT NULL,
  valor       DECIMAL(10,2) NOT NULL,
  observacao  TEXT,
  tipo        TEXT DEFAULT 'A vista' CHECK (tipo IN ('A vista', 'Parcelado')),
  created_at  TIMESTAMPTZ DEFAULT now()
);

CREATE INDEX idx_lancamentos_data     ON lancamentos (data);
CREATE INDEX idx_lancamentos_categoria ON lancamentos (categoria);
```

### Tabela `config`

```sql
CREATE TABLE config (
  chave TEXT PRIMARY KEY,
  valor TEXT NOT NULL
);

INSERT INTO config VALUES
  ('renda_guilherme',  '20000'),
  ('renda_esposa',     '14000'),
  ('meta_fatura',      '12000'),
  ('total_fixos',      '14559'),
  ('teto_alimentacao_fora',        '2000'),
  ('teto_mercado_supermercado',    '1800'),
  ('teto_farmacia_saude',          '800'),
  ('teto_veiculos_transporte',     '1400'),
  ('teto_viagem_lazer',            '1000'),
  ('teto_vestuario_moda',          '1000'),
  ('teto_mercado_livre',           '600'),
  ('teto_streaming_assinaturas',   '370'),
  ('teto_educacao_infanto',        '180'),
  ('teto_pet',                     '450'),
  ('teto_decoracao_casa',          '800'),
  ('teto_guitarra',                '300'),
  ('teto_presentes',               '800'),
  ('teto_outros_diversos',         '500');

INSERT INTO config VALUES
  ('conta_fixa_financiamento_imovel', '7500'),
  ('conta_fixa_escola',                '2300'),
  ('conta_fixa_convenio_medico',       '2000'),
  ('conta_fixa_condominio',            '1600'),
  ('conta_fixa_terapia',               '510'),
  ('conta_fixa_claro',                 '281'),
  ('conta_fixa_vivo',                  '124'),
  ('conta_fixa_seguro_carro',          '244');
```

As contas fixas (bloco `conta_fixa_*`) ficam na mesma tabela `config`, editáveis direto no
Supabase sem precisar de deploy. `GET /api/config` monta o array `contas_fixas` a partir
dessas chaves (ver `CONTA_FIXA_MAP` em `api/config.js`).

---

## API — Vercel Functions

### `GET /api/config`
Retorna configurações: renda, meta, total_fixos, tetos por categoria, contas fixas.

```json
{
  "renda_guilherme": 20000,
  "renda_esposa": 14000,
  "meta_fatura": 12000,
  "total_fixos": 14559,
  "tetos": {
    "Alimentacao fora": 2000,
    "Mercado / Supermercado": 1800,
    ...
  },
  "contas_fixas": [
    { "nome": "Financiamento imóvel", "valor": 7500 },
    { "nome": "Escola (filha)", "valor": 2300 },
    { "nome": "Convênio médico", "valor": 2000 },
    { "nome": "Condomínio", "valor": 1600 },
    { "nome": "Terapia", "valor": 510 },
    { "nome": "Claro", "valor": 281 },
    { "nome": "Vivo", "valor": 124 },
    { "nome": "Seguro carro", "valor": 244 }
  ]
}
```

### `GET /api/lancamentos?mes=2026-06`
Retorna todos os lançamentos do mês. Parâmetro `mes` no formato `YYYY-MM`.

```json
[
  {
    "id": "uuid",
    "data": "2026-06-01",
    "descricao": "Almoço Outback",
    "categoria": "Alimentacao fora",
    "valor": 89.90,
    "observacao": "Aniversário da Laura",
    "tipo": "A vista",
    "created_at": "2026-06-01T14:30:00Z"
  }
]
```

### `POST /api/lancamentos`
Cria um ou múltiplos lançamentos (suporte a parcelamento).

Request body:
```json
{
  "data": "01/06/2026",
  "descricao": "Zarpo viagem",
  "categoria": "Viagem / Lazer",
  "valor": 739.11,
  "observacao": "Porto de Galinhas",
  "tipo": "Parcelado",
  "parcelas": 4
}
```

Se `parcelas > 1`, inserir N registros com datas consecutivas (mês +1 cada).
Valor de cada parcela: `valor / parcelas` (arredondar para 2 casas).
Descrição de cada parcela: `"Zarpo viagem (1/4)"`, `"Zarpo viagem (2/4)"`, etc.

Response: `{ "inserted": 4, "ids": ["uuid1", "uuid2", ...] }`

### `DELETE /api/lancamentos/:id`
Remove lançamento por UUID.

Response: `{ "deleted": true }`

### `PUT /api/lancamentos/:id`
Atualiza campos de um lançamento.

Request body (campos opcionais, atualiza apenas os enviados):
```json
{
  "descricao": "Novo nome",
  "categoria": "Outros / Diversos",
  "valor": 100.00,
  "data": "05/06/2026",
  "tipo": "A vista",
  "observacao": "Observação atualizada"
}
```

Response: `{ "updated": true, "lancamento": { ...campos atualizados } }`

---

## Categorias e tetos

Total: R$ 12.000 exatos.

| Categoria | Teto | Ícone |
|---|---|---|
| Alimentacao fora | R$ 2.000 | 🍽 |
| Mercado / Supermercado | R$ 1.800 | 🛒 |
| Farmacia / Saude | R$ 800 | 💊 |
| Veiculos / Transporte | R$ 1.400 | 🚗 |
| Viagem / Lazer | R$ 1.000 | ✈ |
| Vestuario / Moda | R$ 1.000 | 🛍 |
| Mercado Livre | R$ 600 | 📦 |
| Streaming / Assinaturas | R$ 370 | 📺 |
| Educacao / Infanto | R$ 180 | 📚 |
| Pet | R$ 450 | 🐾 |
| Decoracao Casa | R$ 800 | 🏠 |
| Guitarra | R$ 300 | 🎸 |
| Presentes | R$ 800 | 🎁 |
| Outros / Diversos | R$ 500 | 🎯 |

---

## Meses disponíveis no app

jun/26 → jun/27 (13 meses)

---

## Telas (5) — navegação bottom nav

### 1. Painel
- Cards de resumo: "Gasto no mês" e "Disponível" com cores dinâmicas
- Lista de categorias com barra de progresso + status label + valor gasto vs teto
- Últimos 5 lançamentos do mês (clicáveis → detalhe)
- Botão "Ver todos" → navega para Lançamentos

### 2. Lançar
- Campo valor grande (inputmode="decimal")
- Dropdown categoria (14 opções)
- Campo descrição
- Data (default hoje) + Tipo (À vista / Parcelado)
- Campo Observação (opcional)
- Se Parcelado: campo nº parcelas + preview "7x de R$142,86 · vai até dez/26"
- Botão salvar → POST /api/lancamentos

### 3. Lançamentos
- Card total lançado (cor dinâmica vs meta)
- Barra de filtros horizontal deslizável por categoria (mostra só categorias com entradas)
- Lista de itens clicáveis → abre Detalhe
- Ícone lixeira em cada item → confirmação → DELETE /api/lancamentos/:id
- Trocar mês no header → atualiza lista automaticamente

### 4. Orçamento
- Cards: Sobra/Poupança e Meta poupança (20% da renda)
- Bloco Renda (pessoa 1 + pessoa 2 + total)
- Bloco Contas Fixas (itemizado com os 8 itens)
- Bloco Resultado do mês: fatura cartão, meta, sobra, % poupança

### 5. Histórico
- Card por mês com: total, barra de progresso, badge de status
- Expandido: meta, fixos, renda, sobra/poupança (± valor + %), categorias com gastos
- Botão "Limpar mês" → confirmação → DELETE em lote por mês

---

## Tela de Detalhe (ao clicar em lançamento)

- Botão voltar
- Card hero: ícone categoria, descrição, categoria, valor colorido
- Linhas: Data, Tipo, Observação (se preenchida)
- Botão "Editar" → abre Modal de Edição
- Botão "Excluir" (vermelho) → confirmação → DELETE

---

## Modal de Edição

Campos: Descrição, Categoria (dropdown), Valor, Data, Tipo, Observação
Salvar → PUT /api/lancamentos/:id

---

## Header (fixo no topo)

- Título "Lazzarini 💳" + subtítulo "Controle de gastos"
- Botão seletor de mês → modal com grid de 13 meses
  - Trocar mês atualiza a tela ativa sem reload
- Botão refresh → GET /api/lancamentos?mes=... + re-render

---

## Lógica de cores (consistente em toda a app)

### Por categoria (gasto ÷ teto):
- 🟢 Verde: < 75%
- 🟡 Amarelo: ≥ 75% e ≤ 100%
- 🔴 Vermelho: > 100%

### Fatura total (gasto ÷ meta R$12.000):
- 🟢 Verde: < 75%
- 🟡 Amarelo: ≥ 75% e ≤ 100%
- 🔴 Vermelho: > 100%

### Sobra/Poupança:
- 🟢 Verde: positivo
- 🔴 Vermelho: negativo

### % Poupança:
- 🟢 Verde: ≥ 20%
- 🟡 Amarelo: 10% a 19%
- 🔴 Vermelho: < 10%

---

## Design tokens

```css
--bg:       #0f1117;
--surface:  #181c27;
--surface2: #1e2436;
--border:   #2a3148;
--accent:   #4f8ef7;
--green:    #22c55e;
--amber:    #f59e0b;
--red:      #ef4444;
--text:     #e8eaf0;
--muted:    #8892a4;
```

Fonts: DM Sans (UI) + DM Mono (valores monetários)
Border radius cards: 16px
Nav: fixed bottom, 5 botões com ícones SVG

---

## Migração (Google Sheets → Supabase)

Script `scripts/migrate.js`:

1. Ler aba `lancamentos` do Sheets via Google Sheets API v4 (API Key)
2. Converter datas de `DD/MM/YYYY` para `YYYY-MM-DD` (formato DATE do Postgres)
3. Mapear coluna `estabelecimento` → campo `observacao` (renomeado)
4. Inserir em lotes de 100 no Supabase via `@supabase/supabase-js`
5. Ler aba `config` e popular tabela `config` no Supabase
6. Exibir relatório: total migrado, erros, tempo

Não há endpoint HTTP de migração — a migração roda só localmente via `node scripts/migrate.js`. `MIGRATE_SECRET` fica reservado caso um endpoint `/api/migrate` venha a ser exposto no futuro.

---

## Estrutura de arquivos

```
/
├── index.html               # Frontend completo
├── manifest.json            # PWA manifest
├── sw.js                    # Service worker (cache offline)
├── api/
│   ├── lancamentos.js       # GET, POST de lançamentos
│   ├── lancamentos/[id].js  # PUT, DELETE de um lançamento (rota dinâmica)
│   └── config.js            # Configurações e tetos
├── scripts/
│   └── migrate.js          # Script local de migração (não há endpoint HTTP de migração)
├── vercel.json             # Configuração Vercel
├── package.json            # @supabase/supabase-js
├── CLAUDE.md               # Contexto para Claude Code
└── SPEC.md                 # Este arquivo
```

---

## Checklist de implementação

- [ ] Criar tabelas no Supabase (SQL acima)
- [ ] Configurar variáveis de ambiente no Vercel
- [ ] Implementar `GET /api/config`
- [ ] Implementar `GET /api/lancamentos`
- [ ] Implementar `POST /api/lancamentos` (com suporte a parcelas)
- [ ] Implementar `DELETE /api/lancamentos/:id`
- [ ] Implementar `PUT /api/lancamentos/:id`
- [ ] Implementar `scripts/migrate.js`
- [ ] Atualizar `index.html` — trocar chamadas Sheets API pelas novas rotas `/api/*`
- [ ] Remover dependência de Google Apps Script e Sheets API do frontend
- [ ] Testar migração em ambiente de desenvolvimento
- [ ] Deploy no Vercel e validar em produção
