# Implementação — correções pós-migração Supabase (2026-07)

Registro do que foi executado a partir da `SPEC-correcoes-2026-07.md` (já removida
após conclusão — este arquivo é o resumo permanente do que foi feito).

Contexto: a migração Google Sheets → Supabase + Vercel Functions já estava
funcional, mas com 5 pendências identificadas em validações anteriores. Todas
foram corrigidas e validadas ponta a ponta nesta rodada.

---

## 1. `scripts/migrate.js` — segredos hardcoded removidos

**Antes:** `SUPABASE_URL`, `GOOGLE_SHEETS_ID` e `GOOGLE_API_KEY` tinham fallbacks
literais no código-fonte (incluindo uma API key real do Google e um Project ID
do Supabase incorreto).

**Depois:** as 4 variáveis usadas pelo script (`SUPABASE_URL`,
`SUPABASE_SERVICE_KEY`, `GOOGLE_SHEETS_ID`, `GOOGLE_API_KEY`) são exigidas via
`process.env`, sem fallback — o script agora falha com `process.exit(1)` e uma
mensagem clara se qualquer uma estiver ausente. Nenhuma dessas 4 variáveis foi
adicionada a `.env.local` (uso é pontual, só na hora de rodar a migração).

---

## 2. `contas_fixas` migrado de hardcode para a tabela `config`

**Antes:** `api/config.js` retornava os 8 itens de contas fixas (Financiamento
imóvel, Escola, Convênio médico, Condomínio, Terapia, Claro, Vivo, Seguro carro)
como array constante no próprio arquivo. Alterar qualquer valor exigia deploy.

**Depois:**
- Tabela `config` (schema chave/valor) ganhou 8 novas chaves `conta_fixa_*`,
  seguindo o mesmo padrão já usado para os tetos por categoria (`teto_*`).
- `api/config.js` monta `contas_fixas` a partir dessas chaves via
  `CONTA_FIXA_MAP` (mesmo padrão do `TETO_MAP` já existente), em vez do array
  hardcoded.
- `supabase-setup.sql` atualizado com o `INSERT` correspondente, para que uma
  reinstalação do banco já venha com os dados corretos.
- O `INSERT` foi rodado manualmente no SQL Editor do Supabase (não era possível
  via `service_role` — a tabela `config` só tem `GRANT SELECT` para essa role,
  por design, já que o app nunca escreve em `config`).
- `SPEC.md` atualizado refletindo a nova fonte de dados.

Resultado: as contas fixas agora são editáveis direto no Supabase, sem depender
de deploy. Validado via `GET /api/config` — os 8 valores batem exatamente com
os originais (7500, 2300, 2000, 1600, 510, 281, 124, 244).

---

## 3. Roteamento de `/api/lancamentos/:id` corrigido

**Antes:** `api/lancamentos.js` extraía o `id` manualmente de `req.url`,
esperando receber `/api/lancamentos/{uuid}` para PUT/DELETE. Mas o Vercel roteia
funções por arquivo — um único arquivo `api/lancamentos.js` só responde a
`/api/lancamentos` exato. O único rewrite em `vercel.json`
(`/api/(.*)` → `/api/$1`) era um no-op (destino idêntico à origem) e não
resolvia isso. Na prática, PUT e DELETE de lançamentos (editar, excluir,
"Limpar mês") provavelmente retornavam 404 em produção — isso nunca tinha sido
testado de fato, só ficou marcado como "bloqueado" em validações anteriores.

**Depois:**
- Criado `api/lancamentos/[id].js` — rota dinâmica nativa do Vercel, lendo o
  `id` via `req.query.id`. Contém a lógica de PUT e DELETE.
- `api/lancamentos.js` agora só responde GET e POST (CORS ajustado de acordo).

**Validado com `vercel dev` real** (não só leitura de código):
- `POST /api/lancamentos` (à vista) → 201, registro criado.
- `PUT /api/lancamentos/:id` → 200, sem 404, edição persistida e conferida via GET.
- `DELETE /api/lancamentos/:id` → 200, sem 404.
- `POST /api/lancamentos` com `parcelas: 3` → 3 registros em meses consecutivos
  (jul/ago/set 2026), valor dividido corretamente (300 → 100 cada), descrição
  sufixada `(1/3)`, `(2/3)`, `(3/3)`.
- Todos os dados de teste foram apagados ao final (banco ficou limpo).

---

## 4. Limpeza de documentação obsoleta

- `.claude/SETUP.md` removido (instruções de configuração de abas do Google
  Sheets, legado pré-Supabase).
- `CLAUDE.md` e `SPEC.md`: removida a menção a um endpoint `api/migrate.js` que
  nunca existiu — só há `scripts/migrate.js` (script local, sem endpoint HTTP).
- `CLAUDE.md` e `SPEC.md`: árvore de arquivos atualizada para incluir
  `manifest.json`, `sw.js` e a nova rota `api/lancamentos/[id].js`.

---

## 5. Validação final

Rodado `npx vercel dev` localmente (autenticado via token da Vercel) e testados
os endpoints reais contra o Supabase de produção:

| Endpoint | Resultado |
|---|---|
| `GET /api/config` | ✅ 200 — `contas_fixas` vindo do banco, 8 valores conferidos |
| `GET /api/lancamentos?mes=YYYY-MM` | ✅ 200 |
| `POST /api/lancamentos` (à vista) | ✅ 201 |
| `POST /api/lancamentos` (parcelado) | ✅ 201 — datas, valores e descrições corretos |
| `PUT /api/lancamentos/:id` | ✅ 200 — confirma fix do item 3 |
| `DELETE /api/lancamentos/:id` | ✅ 200 — confirma fix do item 3 |

Não foram testadas as 5 telas no navegador nesta rodada (fora do escopo desta
sessão de correções — só API).

---

## Estado após esta implementação

Todas as 5 pendências identificadas nas validações anteriores foram resolvidas:
segredos hardcoded, dado editável fora de deploy, roteamento de rota dinâmica,
documentação desatualizada, e ausência de teste end-to-end real dos endpoints
críticos (PUT/DELETE). Não há pendências conhecidas em aberto neste momento.
