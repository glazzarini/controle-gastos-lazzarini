-- ============================================================
-- SETUP DO BANCO — controle-gastos
-- Executar no Supabase SQL Editor
-- ============================================================

-- Tabela de lançamentos
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

CREATE INDEX idx_lancamentos_data      ON lancamentos (data);
CREATE INDEX idx_lancamentos_categoria ON lancamentos (categoria);

-- Tabela de configuração
CREATE TABLE config (
  chave TEXT PRIMARY KEY,
  valor TEXT NOT NULL
);

-- Dados iniciais de configuração
INSERT INTO config VALUES
  ('renda_guilherme',               '20000'),
  ('renda_esposa',                  '14000'),
  ('meta_fatura',                   '12000'),
  ('total_fixos',                   '14559'),
  ('teto_alimentacao_fora',         '2000'),
  ('teto_mercado_supermercado',     '1800'),
  ('teto_farmacia_saude',           '800'),
  ('teto_veiculos_transporte',      '1400'),
  ('teto_viagem_lazer',             '1000'),
  ('teto_vestuario_moda',           '1000'),
  ('teto_mercado_livre',            '600'),
  ('teto_streaming_assinaturas',    '370'),
  ('teto_educacao_infanto',         '180'),
  ('teto_pet',                      '450'),
  ('teto_decoracao_casa',           '800'),
  ('teto_guitarra',                 '300'),
  ('teto_presentes',                '800'),
  ('teto_outros_diversos',          '500');

-- Contas fixas (itemizadas, editáveis direto no Supabase sem precisar de deploy)
INSERT INTO config VALUES
  ('conta_fixa_financiamento_imovel', '7500'),
  ('conta_fixa_escola',                '2300'),
  ('conta_fixa_convenio_medico',       '2000'),
  ('conta_fixa_condominio',            '1600'),
  ('conta_fixa_terapia',               '510'),
  ('conta_fixa_claro',                 '281'),
  ('conta_fixa_vivo',                  '124'),
  ('conta_fixa_seguro_carro',          '244');

-- Privilégios — sem isso o service_role (usado pelas Vercel Functions)
-- recebe "permission denied for table" em toda query.
GRANT SELECT, INSERT, UPDATE, DELETE ON public.lancamentos TO service_role;
GRANT SELECT                         ON public.config      TO service_role;
