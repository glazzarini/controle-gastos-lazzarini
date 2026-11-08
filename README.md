# controle-gastos — Lazzarini

PWA de controle de gastos do cartão de crédito da família Lazzarini.

| Camada | Stack |
|---|---|
| Frontend | React 19 + Vite + TypeScript + `vite-plugin-pwa`, servido por Nginx |
| Backend | ASP.NET Core Web API (.NET 10, Controllers) + Dapper + Npgsql |
| Banco | Supabase (Postgres) — schema em `supabase-setup.sql` + `supabase-migration-categorias-contas-fixas.sql` |
| Hospedagem | Kubernetes (Kind local; DOKS em SPEC futura) |

Contrato HTTP: `/api/*`, `snake_case`, datas `DD/MM/YYYY`, erros `{ error }` — idêntico ao da versão anterior (Vercel), preservada na branch local `backup/legado-vercel`.

---

## Backend (`backend/`)

```
backend/
├── ControleGastos.sln
├── src/ControleGastos.Api    # Controllers, Program.cs, middlewares (erro/CORS), JSON, health checks
├── src/ControleGastos.Core   # Entities, Dtos, Interfaces, Services, Repositories (Dapper), Mappings
└── tests/ControleGastos.Test # xUnit + NSubstitute + Testcontainers (Postgres) + WebApplicationFactory
```

Requisitos: .NET SDK 10 e Docker (os testes de integração sobem um Postgres com o DDL real).

```bash
cd backend
dotnet build
dotnet test                                   # 435 testes
dotnet test -p:CollectCoverage=true           # cobertura (falha abaixo de 100% em linha/branch/método)
dotnet tool restore && dotnet tool run reportgenerator \
  -reports:tests/ControleGastos.Test/TestResults/coverage/coverage.cobertura.xml \
  -targetdir:tests/ControleGastos.Test/TestResults/coverage/html   # relatório HTML
```

Rodar localmente (a connection string **nunca** vai para o `appsettings.json` nem para o git):

```bash
cd backend/src/ControleGastos.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Supabase" "Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require"
dotnet run                                    # http://localhost:8080
```

Supabase: usar o **Session pooler** (IPv4) — `Host=aws-1-sa-east-1.pooler.supabase.com;Port=5432;Username=postgres.<project-ref>;SSL Mode=Require;Pooling=true;Maximum Pool Size=5` (host em Supabase → **Connect** → Connection String → Session pooler). Requisições aparecem no log do container: `kubectl logs -f -n controle-gastos deploy/backend`.

Health checks (fora de `/api`): `GET /healthz` (liveness, não toca no banco) e `GET /readyz` (readiness, `SELECT 1`).

## Frontend (`frontend/`)

Requisitos: Node 24 (`frontend/.nvmrc` → `nvm use`).

```bash
cd frontend
nvm use
npm ci
npm run dev                # http://localhost:5173 (proxy de /api → http://localhost:8080)
npm test                   # 108 testes (Vitest + Testing Library + MSW)
npm run test:coverage      # falha abaixo de 100%
npm run build              # tsc -b + vite build (gera o PWA em dist/)
```

---

## Kubernetes local (Kind)

```
k8s/
├── base/                    # reaproveitada no DOKS: deployments, services, ingress, namespace
│   └── secret.example.yaml  # modelo — o secret real é criado com kubectl e nunca vai para o git
└── overlays/kind/
    ├── kustomization.yaml   # imagens locais (:local) + Postgres local
    ├── kind-cluster.yaml    # portas 80/443 do host → ingress-nginx
    └── postgres/            # Postgres só do Kind (DDL real + dados fictícios)
```

Requisitos: Docker, `kind`, `kubectl`. Portas 80 e 443 livres.

```bash
# 1. Cluster
kind create cluster --config k8s/overlays/kind/kind-cluster.yaml

# 2. ingress-nginx para Kind
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.15.1/deploy/static/provider/kind/deploy.yaml
kubectl -n ingress-nginx wait --for=condition=ready pod -l app.kubernetes.io/component=controller --timeout=300s

# 3. Imagens (build local + carga no Kind, sem registry)
docker build -t controle-gastos-backend:local backend/
docker build -t controle-gastos-frontend:local frontend/
docker pull postgres:17-alpine
for img in controle-gastos-backend:local controle-gastos-frontend:local postgres:17-alpine; do
  kind load docker-image "$img" --name controle-gastos
done

# 4. Namespace, scripts do banco local e secret com a connection string
kubectl create namespace controle-gastos
kubectl -n controle-gastos create configmap postgres-init \
  --from-file=00-role.sql=k8s/overlays/kind/postgres/00-role.sql \
  --from-file=10-setup.sql=supabase-setup.sql \
  --from-file=20-migration.sql=supabase-migration-categorias-contas-fixas.sql \
  --from-file=90-seed-dev.sql=k8s/overlays/kind/postgres/90-seed-dev.sql
kubectl -n controle-gastos create secret generic controle-gastos-db \
  --from-literal=connection-string='Host=postgres;Port=5432;Database=postgres;Username=postgres;Password=dev-local;Pooling=true;Maximum Pool Size=5;Timeout=15'

# 5. Manifests
kubectl apply -k k8s/overlays/kind
kubectl -n controle-gastos wait --for=condition=available deploy --all --timeout=300s

# 6. Conferência: pods Running/Ready, probes e memória
kubectl -n controle-gastos get pods
kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/download/v0.9.1/components.yaml
kubectl -n kube-system patch deploy metrics-server --type=json \
  -p='[{"op":"add","path":"/spec/template/spec/containers/0/args/-","value":"--kubelet-insecure-tls"}]'
kubectl top pods -n controle-gastos
```

App: http://localhost/ (front) e http://localhost/api/config (API).

Para apontar para o Supabase em vez do Postgres local, recrie o secret com a connection string do Session pooler (`kubectl -n controle-gastos delete secret controle-gastos-db` + `create secret ...`) e reinicie o backend (`kubectl -n controle-gastos rollout restart deploy/backend`).

Atualizar após mudanças no código: refazer o build, `kind load docker-image ...` e `kubectl -n controle-gastos rollout restart deploy/backend deploy/frontend`.

Remover tudo: `kind delete cluster --name controle-gastos`.
