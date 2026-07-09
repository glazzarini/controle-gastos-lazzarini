# controle-gastos

PWA de controle de gastos do cartão de crédito da família Lazzarini.
Stack: Node.js + Vercel Serverless Functions + Supabase (PostgreSQL) + HTML/CSS/JS puro no frontend.

## Comandos

```bash
# Instalar dependências
npm install

# Dev local (Vercel CLI)
npx vercel dev

# Deploy
npx vercel --prod

# Rodar migração de dados (Google Sheets → Supabase)
node scripts/migrate.js

# Verificar variáveis de ambiente
npx vercel env ls
```

## Arquitetura

```
/
├── index.html               # Frontend completo (single file, JS puro)
├── manifest.json            # PWA manifest
├── sw.js                    # Service worker (cache offline)
├── api/
│   ├── lancamentos.js       # GET /api/lancamentos, POST
│   ├── lancamentos/[id].js  # PUT /api/lancamentos/:id, DELETE (rota dinâmica)
│   └── config.js            # GET /api/config (categorias, tetos, renda, fixos)
├── scripts/
│   └── migrate.js           # Script local de migração Sheets → Supabase (não há endpoint HTTP de migração)
├── vercel.json
└── SPEC.md                  # Especificação completa do produto
```

## Variáveis de ambiente (Vercel)

```
SUPABASE_URL=https://hczdfittcvtxbivzvcfp.supabase.co
SUPABASE_SERVICE_KEY=      # secret key (nunca expor no frontend)
SUPABASE_ANON_KEY=         # publishable key (usada no frontend via /api)
GOOGLE_SHEETS_ID=          # apenas para migração
GOOGLE_API_KEY=            # apenas para migração
MIGRATE_SECRET=            # token de proteção do endpoint de migração
```

Nunca expor SUPABASE_SERVICE_KEY no frontend. Todo acesso ao banco passa pelas Vercel Functions.

## Banco de dados (Supabase)

Duas tabelas — ver SPEC.md para o SQL completo:
- `lancamentos` — registros de gastos
- `config` — pares chave/valor (renda, meta, tetos por categoria)

Row Level Security (RLS): desabilitado por ora (app sem auth). Habilitar se adicionar login no futuro.

## Convenções do código

- Vercel Functions: cada arquivo em `/api/` exporta `export default async function handler(req, res)`
- Frontend: sem frameworks, sem bundler — JS puro com `fetch()` para as APIs
- Datas: sempre trafegar como string `DD/MM/YYYY` entre frontend e API; armazenar como `DATE` no Postgres
- Valores monetários: `DECIMAL(10,2)` no banco, `number` em JS — nunca string
- IDs: UUID gerado pelo Postgres (`gen_random_uuid()`)
- CORS: headers de CORS nas Vercel Functions, não no frontend
- Erros: sempre retornar `{ error: string }` com status HTTP adequado (400, 404, 500)

## Anti-padrões conhecidos

- Não chamar Supabase diretamente do frontend — sempre via `/api/*`
- Não usar `no-cors` em fetch — causa problemas silenciosos de CORS
- Não formatar datas com `TEXT()` do Google Sheets (locale-dependente) — usar `DATE` nativo do Postgres
- Não misturar `estabelecimento` e `observacao` — campo unificado é `observacao`

## Contexto do produto

Ver SPEC.md para: telas, categorias, tetos, lógica de cores, orçamento familiar e fluxos completos.
