-- Dados FICTÍCIOS para a regressão local no Kind (nunca usar em produção).
-- Categorias, contas fixas e config vêm de supabase-setup.sql + supabase-migration-categorias-contas-fixas.sql.
INSERT INTO lancamentos (data, descricao, categoria, valor, observacao, tipo) VALUES
  ('2026-11-02', 'Almoço Outback',        'Alimentacao fora',        189.90, 'Aniversário',      'A vista'),
  ('2026-11-03', 'Supermercado semanal',  'Mercado / Supermercado',  642.35, NULL,               'A vista'),
  ('2026-11-04', 'Ração',                 'Pet',                     380.00, 'Petz',             'A vista'),
  ('2026-11-05', 'Estorno Mercado Livre', 'Mercado Livre',           -89.90, 'Crédito na fatura','A vista'),
  ('2026-11-06', 'Cordas',                'Guitarra',                350.00, NULL,               'A vista'),
  ('2026-11-07', 'Netflix',               'Streaming / Assinaturas',  55.90, NULL,               'A vista'),
  ('2026-11-07', 'Farmácia',              'Farmacia / Saude',        120.00, NULL,               'A vista'),
  ('2026-10-10', 'TV (1/3)',              'Decoracao Casa',          999.67, NULL,               'Parcelado'),
  ('2026-11-10', 'TV (2/3)',              'Decoracao Casa',          999.67, NULL,               'Parcelado'),
  ('2026-12-10', 'TV (3/3)',              'Decoracao Casa',          999.67, NULL,               'Parcelado'),
  ('2026-10-15', 'Viagem Gramado',        'Viagem / Lazer',         2400.00, NULL,               'A vista'),
  ('2026-09-20', 'Combustível',           'Veiculos / Transporte',   300.00, NULL,               'A vista'),
  ('2026-08-05', 'Presente',              'Presentes',               150.00, NULL,               'A vista'),
  ('2026-06-12', 'Categoria antiga',      'Sem teto (legado)',        40.00, NULL,               'A vista');
