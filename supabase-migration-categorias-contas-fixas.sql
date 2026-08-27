-- ============================================================
-- MIGRAÇÃO — categorias e contas fixas dinâmicas (CRUD)
-- Executar no Supabase SQL Editor, depois de supabase-setup.sql
--
-- Substitui os pares fixos "teto_*" e "conta_fixa_*" da tabela config
-- por tabelas próprias, permitindo criar/editar/excluir categorias e
-- contas fixas pelo app sem precisar de deploy.
-- ============================================================

-- Tabela de categorias (nome + teto)
CREATE TABLE categorias (
  id         UUID DEFAULT gen_random_uuid() PRIMARY KEY,
  nome       TEXT NOT NULL UNIQUE,
  teto       DECIMAL(10,2) NOT NULL DEFAULT 0,
  created_at TIMESTAMPTZ DEFAULT now()
);

-- Tabela de contas fixas (nome + valor)
CREATE TABLE contas_fixas (
  id         UUID DEFAULT gen_random_uuid() PRIMARY KEY,
  nome       TEXT NOT NULL,
  valor      DECIMAL(10,2) NOT NULL DEFAULT 0,
  created_at TIMESTAMPTZ DEFAULT now()
);

GRANT SELECT, INSERT, UPDATE, DELETE ON public.categorias   TO service_role;
GRANT SELECT, INSERT, UPDATE, DELETE ON public.contas_fixas TO service_role;

-- Seed com os valores que hoje estão hardcoded em api/config.js (TETO_MAP)
INSERT INTO categorias (nome, teto) VALUES
  ('Alimentacao fora',         2000),
  ('Mercado / Supermercado',   1800),
  ('Farmacia / Saude',          800),
  ('Veiculos / Transporte',    1400),
  ('Viagem / Lazer',           1000),
  ('Vestuario / Moda',         1000),
  ('Mercado Livre',             600),
  ('Streaming / Assinaturas',   370),
  ('Educacao / Infanto',        180),
  ('Pet',                       450),
  ('Decoracao Casa',            800),
  ('Guitarra',                  300),
  ('Presentes',                 800),
  ('Outros / Diversos',         500);

-- Seed com os valores que hoje estão hardcoded em api/config.js (CONTA_FIXA_MAP)
INSERT INTO contas_fixas (nome, valor) VALUES
  ('Financiamento imóvel', 7500),
  ('Escola (filha)',       2300),
  ('Convênio médico',      2000),
  ('Condomínio',           1600),
  ('Terapia',                510),
  ('Claro',                  281),
  ('Vivo',                   124),
  ('Seguro carro',           244);

-- As chaves antigas teto_*/conta_fixa_*/total_fixos deixam de ser lidas pela
-- API (api/config.js agora lê de categorias/contas_fixas e calcula total_fixos
-- por SUM). Removidas para não ficar dado morto/duplicado na tabela config.
DELETE FROM config WHERE chave LIKE 'teto_%' OR chave LIKE 'conta_fixa_%' OR chave = 'total_fixos';
