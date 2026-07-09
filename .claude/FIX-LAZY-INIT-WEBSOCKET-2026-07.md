# Fix — lazy init do Supabase client + WebSocket em Node 18 (2026-07-09)

Registro das correções aplicadas depois da implementação da `SPEC-correcoes-2026-07.md`
(ver `IMPLEMENTACAO-2026-07.md`), motivadas por dois bugs encontrados ao rodar
`vercel dev` de verdade contra o Supabase real.

---

## 1. Problema original: `createClient()` no nível de módulo

**Sintoma:** ao subir `vercel dev`, o erro `supabaseUrl is required` aparecia
repetidas vezes no boot, antes de qualquer requisição chegar.

**Causa:** `api/lancamentos.js`, `api/lancamentos/[id].js` e `api/config.js`
chamavam `createClient(process.env.SUPABASE_URL, ...)` no topo do arquivo (nível
de módulo). Nesse momento do ciclo de vida da function — bundling/análise do
arquivo pelo Vercel — o `process.env` ainda não estava totalmente injetado, então
`createClient()` recebia `undefined` como URL e falhava antes mesmo do handler
rodar.

**Fix:** criada uma função `getSupabase()` em cada um dos 3 arquivos, que só
chama `createClient()` na primeira vez que é invocada de dentro do `handler`
(lazy init, com cache em variável de módulo `let supabase`):

```js
let supabase;
function getSupabase() {
  if (!supabase) {
    supabase = createClient(process.env.SUPABASE_URL, process.env.SUPABASE_SERVICE_KEY, {
      realtime: { transport: ws },
    });
  }
  return supabase;
}
```

Todas as chamadas `supabase.from(...)` dentro dos handlers foram trocadas por
`getSupabase().from(...)`.

**Arquivos alterados:** `api/config.js`, `api/lancamentos.js`,
`api/lancamentos/[id].js`.

**Nota:** `scripts/migrate.js` **não** recebeu esse lazy init — é um script
standalone que roda uma vez só (não uma serverless function reaproveitada entre
requisições), então `createClient()` no topo do arquivo não tem o mesmo problema
de timing.

---

## 2. Problema novo, descoberto ao testar após o fix #1: WebSocket ausente em Node 18

**Sintoma:** com o lazy init já aplicado, `GET /api/config` passou a retornar
`500` com a mensagem:
```
Node.js 18 detected without native WebSocket support.
Suggested solution: For Node.js < 22, install "ws" package and provide it via
the transport option: import ws from "ws"; new RealtimeClient(url, { transport: ws })
```

**Causa raiz:** não tinha relação com o fix #1. O `package.json` foi atualizado
externamente (`vercel` `^37.0.0` → `^54.21.1`), o que disparou um `npm install`
que trouxe uma versão mais nova do `@supabase/supabase-js` (2.109.0, dentro do
range `^2.45.0` já declarado). Nessa versão, o construtor do `SupabaseClient`
sempre instancia um `RealtimeClient` internamente — mesmo que a aplicação nunca
use realtime/subscriptions — e essa instanciação valida sincronamente se há
suporte nativo a `WebSocket` no ambiente. Node 18 não tem (só a partir do Node
22), então `createClient()` lança exceção imediatamente, independente de ser
chamado no nível de módulo ou dentro do handler (lazy init não resolve esse
problema).

**Fix:** instalado `ws` (`npm install ws --save`, versão `^8.21.0`) como
dependência direta em `package.json`, e passada a opção `transport` para
`createClient()` em todos os 4 arquivos que o chamam:

```js
import ws from 'ws';
// ...
createClient(url, key, { realtime: { transport: ws } });
```

**Arquivos alterados:** `api/config.js`, `api/lancamentos.js`,
`api/lancamentos/[id].js`, `scripts/migrate.js`.

**Por que instalar `ws` em vez de outra solução:** `ws` já existia em
`node_modules` como dependência transitiva do próprio Vercel CLI, mas não
estava declarado como dependência direta do projeto — depender disso seria
frágil (podia sumir numa atualização do CLI). Alternativas descartadas:
downgrade do `@supabase/supabase-js` (contradiz o range de versão já aceito
pelo projeto) e manter sem realtime desabilitável (não existe flag de
opt-out no construtor — o `RealtimeClient` é sempre instanciado).

---

## Diff completo

### `api/config.js`
```diff
 import { createClient } from '@supabase/supabase-js';
+import ws from 'ws';

-const supabase = createClient(
-  process.env.SUPABASE_URL,
-  process.env.SUPABASE_SERVICE_KEY
-);
+let supabase;
+function getSupabase() {
+  if (!supabase) {
+    supabase = createClient(process.env.SUPABASE_URL, process.env.SUPABASE_SERVICE_KEY, {
+      realtime: { transport: ws },
+    });
+  }
+  return supabase;
+}
 ...
-    const { data, error } = await supabase.from('config').select('*');
+    const { data, error } = await getSupabase().from('config').select('*');
```

### `api/lancamentos.js`
```diff
 import { createClient } from '@supabase/supabase-js';
-
-const supabase = createClient(
-  process.env.SUPABASE_URL,
-  process.env.SUPABASE_SERVICE_KEY
-);
+import ws from 'ws';
+
+let supabase;
+function getSupabase() {
+  if (!supabase) {
+    supabase = createClient(process.env.SUPABASE_URL, process.env.SUPABASE_SERVICE_KEY, {
+      realtime: { transport: ws },
+    });
+  }
+  return supabase;
+}
 ...
-      let query = supabase
+      let query = getSupabase()
 ...
-      const { data, error } = await supabase
+      const { data, error } = await getSupabase()
         .from('lancamentos')
         .insert(rows)
```

### `api/lancamentos/[id].js`
```diff
 import { createClient } from '@supabase/supabase-js';
+import ws from 'ws';

-const supabase = createClient(
-  process.env.SUPABASE_URL,
-  process.env.SUPABASE_SERVICE_KEY
-);
+let supabase;
+function getSupabase() {
+  if (!supabase) {
+    supabase = createClient(process.env.SUPABASE_URL, process.env.SUPABASE_SERVICE_KEY, {
+      realtime: { transport: ws },
+    });
+  }
+  return supabase;
+}
 ...
-      const { error } = await supabase
+      const { error } = await getSupabase()
         .from('lancamentos')
         .delete()
 ...
-      const { data, error } = await supabase
+      const { data, error } = await getSupabase()
         .from('lancamentos')
         .update(updates)
```

### `scripts/migrate.js`
```diff
 import { createClient } from '@supabase/supabase-js';
+import ws from 'ws';
 ...
-const supabase = createClient(SUPABASE_URL, SUPABASE_SERVICE_KEY);
+const supabase = createClient(SUPABASE_URL, SUPABASE_SERVICE_KEY, {
+  realtime: { transport: ws },
+});
```

### `package.json`
```diff
   "dependencies": {
-    "@supabase/supabase-js": "^2.45.0"
+    "@supabase/supabase-js": "^2.45.0",
+    "ws": "^8.21.0"
   },
   "devDependencies": {
-    "vercel": "^37.0.0"
+    "vercel": "^54.21.1"
   }
```
(`vercel` `^37.0.0` → `^54.21.1`: alteração externa, não feita nesta sessão —
foi essa mudança que disparou o `npm install` que trouxe o `@supabase/supabase-js`
mais novo e expôs o bug #2. `ws` `^8.21.0`: adicionado nesta sessão.)

---

## Validação

Todos os testes rodados com `vercel dev` real (não só leitura de código),
reiniciado do zero após cada correção:

| Teste | Resultado |
|---|---|
| Boot do `vercel dev` sem erros no log | ✅ (antes: 4x `supabaseUrl is required`; depois do fix #2: 0 erros) |
| `GET /api/config` | ✅ 200 — `contas_fixas` com os 8 valores corretos vindos do banco |
| `GET /api/lancamentos?mes=YYYY-MM` | ✅ 200 |
| `POST /api/lancamentos` (à vista) | ✅ 201 |
| `PUT /api/lancamentos/:id` | ✅ 200 — via `api/lancamentos/[id].js` |
| `DELETE /api/lancamentos/:id` | ✅ 200 — via `api/lancamentos/[id].js` |

Dados de teste criados durante a validação foram apagados ao final de cada
rodada — banco ficou limpo.

---

## Estado após esta correção

Nenhuma pendência conhecida. Os 4 arquivos que chamam `createClient()`
(`api/config.js`, `api/lancamentos.js`, `api/lancamentos/[id].js`,
`scripts/migrate.js`) estão consistentes entre si quanto ao transport do
Supabase Realtime, e as 3 serverless functions usam lazy init.
